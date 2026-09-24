using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ZOVserver.Shared.Contracts.Generator;

[Generator]
public sealed class LaserSerializationGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor DiagNotPartial = new(
        "LASER001",
        "The class must be partial",
        "The class '{0}' is marked as serializable, but it is not declared as partial",
        "LaserGenerator",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor DiagInvalidFlag = new(
        "LASER002",
        "Incorrect configuration of FieldAttribute flags",
        "Property '{0}': {1}",
        "LaserGenerator",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor DiagDebug = new(
        "LASERDBG",
        "LaserGenerator",
        "{0}",
        "LaserGenerator",
        DiagnosticSeverity.Warning,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => (ClassDeclarationSyntax)ctx.Node
            )
            .Where(classDecl => classDecl.AttributeLists.Count > 0);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(classProvider.Collect()), (spc, source) =>
        {
            var (compilation, classes) = source;

            foreach (var classDecl in classes)
            {
                var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
                if (model.GetDeclaredSymbol(classDecl) is not { } classSymbol) continue;

                if (!HasSerializableClassAttribute(classSymbol)) continue;

                if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(DiagNotPartial, classDecl.Identifier.GetLocation(),
                        classSymbol.Name));
                    continue;
                }

                try
                {
                    var props = CollectSerializableProperties(classSymbol);
                    if (props.Count == 0) continue;

                    props.Sort((a, b) => a.Order.CompareTo(b.Order));

                    var generatedSource = GenerateForClass(classSymbol, props);
                    spc.AddSource($"{classSymbol.Name}_LaserSerialization.g.cs",
                        SourceText.From(generatedSource, Encoding.UTF8));
                }
                catch (GenError ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(DiagInvalidFlag, classDecl.Identifier.GetLocation(),
                        ex.PropertyName, ex.Message));
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(DiagDebug, Location.None,
                        $"Gen {classSymbol.Name}: {ex.Message}"));
                }
            }
        });
    }

    private static bool HasSerializableClassAttribute(INamedTypeSymbol cls)
    {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var a in cls.GetAttributes())
        {
            var n = a.AttributeClass?.Name;

            if (n is "LaserSerializableAttribute" or "LaserSerializable")
                return true;
        }

        return false;
    }

    private static int GetInheritanceType(INamedTypeSymbol cls)
    {
        var current = cls.BaseType;

        while (current != null)
        {
            var name = current.Name;

            switch (name)
            {
                case "PiranhaMessage":
                    return 1;
                case "LogicCommand":
                    return 2;
                default:
                    current = current.BaseType;
                    break;
            }
        }

        return 0;
    }

    private static bool IsNumeric(FieldKind kind)
    {
        return kind is FieldKind.I8 or FieldKind.U8
            or FieldKind.I16 or FieldKind.U16
            or FieldKind.I32 or FieldKind.U32
            or FieldKind.I64 or FieldKind.U64
            or FieldKind.I128 or FieldKind.U128
            or FieldKind.VInt32 or FieldKind.VInt64
            or FieldKind.DataRef;
    }


    private static bool IsIntegerIType(ITypeSymbol t)
    {
        return t.SpecialType switch
        {
            SpecialType.System_SByte or SpecialType.System_Byte or
                SpecialType.System_Int16 or SpecialType.System_UInt16 or
                SpecialType.System_Int32 or SpecialType.System_UInt32 or
                SpecialType.System_Int64 or SpecialType.System_UInt64 => true,
            _ => t.ToDisplayString() is "System.Int128" or "System.UInt128"
        };
    }

    private static bool IsString(ITypeSymbol t)
    {
        return t.SpecialType == SpecialType.System_String;
    }

    private static bool IsBool(ITypeSymbol t)
    {
        return t.SpecialType == SpecialType.System_Boolean;
    }

    private static bool IsByteArray(ITypeSymbol t, out ITypeSymbol? elem)
    {
        if (t is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte } at)
        {
            elem = at.ElementType;
            return true;
        }

        elem = null;
        return false;
    }

    private static bool IsList(ITypeSymbol t, out ITypeSymbol? elem)
    {
        elem = null;
        if (t is not INamedTypeSymbol { IsGenericType: true } nt) return false;

        var gen = nt.ConstructedFrom.ToDisplayString();

        if (gen is not ("System.Collections.Generic.List<T>" or "System.Collections.Generic.IList<T>"
            or "System.Collections.Generic.IReadOnlyList<T>")) return false;

        elem = nt.TypeArguments[0];
        return true;
    }

    private static bool IsArray(ITypeSymbol t, out ITypeSymbol? elem)
    {
        if (t is IArrayTypeSymbol at)
        {
            elem = at.ElementType;
            return true;
        }

        elem = null;
        return false;
    }

    private static FieldKind DetectFieldKind(ITypeSymbol type, string propName, bool explicitAsDataRef,
        bool writeLength, bool explicitCompressed, bool explicitVarLen)
    {
        if (IsDateTime(type)) return FieldKind.DateTime;
        if (IsString(type)) return explicitCompressed || Ends(propName, "Compressed") ? FieldKind.CStr : FieldKind.Str;
        if (IsBool(type)) return FieldKind.Bool;
        if (IsByteArray(type, out _))
            return !writeLength || Ends(propName, "Raw") ? FieldKind.BytesRaw : FieldKind.Bytes;
        if (IsArray(type, out _)) return FieldKind.Array;
        if (IsList(type, out _)) return FieldKind.List;
        if (!IsIntegerIType(type)) return FieldKind.Obj;

        if (explicitAsDataRef || Ends(propName, "DataRef") || Ends(propName, "GlobalId"))
            return FieldKind.DataRef;

        if (explicitVarLen || Ends(propName, "VInt"))
            return type.SpecialType is SpecialType.System_Int64 or SpecialType.System_UInt64
                ? FieldKind.VInt64
                : FieldKind.VInt32;

        return type.SpecialType switch
        {
            SpecialType.System_SByte => FieldKind.I8,
            SpecialType.System_Byte => FieldKind.U8,
            SpecialType.System_Int16 => FieldKind.I16,
            SpecialType.System_UInt16 => FieldKind.U16,
            SpecialType.System_Int32 => FieldKind.I32,
            SpecialType.System_UInt32 => FieldKind.U32,
            SpecialType.System_Int64 => FieldKind.I64,
            SpecialType.System_UInt64 => FieldKind.U64,
            _ => type.ToDisplayString() switch
            {
                "System.Int128" => FieldKind.I128,
                "System.UInt128" => FieldKind.U128,
                _ => FieldKind.I32
            }
        };

        static bool Ends(string name, string suffix)
        {
            return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool IsDateTime(ITypeSymbol t)
    {
        return t.SpecialType == SpecialType.System_DateTime;
    }

    private static string? TryGetAttrSimpleName(AttributeData ad)
    {
        var n = ad.AttributeClass?.Name;
        if (n is null) return null;
        return n.EndsWith("Attribute", StringComparison.Ordinal) ? n[..^9] : n;
    }

    private static T GetNamed<T>(AttributeData a, string key, T def)
    {
        foreach (var na in a.NamedArguments.Where(na => na.Key == key))
        {
            if (typeof(T).IsArray && na.Value.Kind == TypedConstantKind.Array)
            {
                var values = na.Value.Values;

                var result = Array.CreateInstance(typeof(T).GetElementType()!, values.Length);
                for (var i = 0; i < values.Length; i++)
                    result.SetValue(values[i].Value, i);

                return (T)(object)result;
            }

            if (na.Value.Value is T typedValue)
                return typedValue;
        }

        return def;
    }

    private static string GetEnumName(AttributeData a, string key, string def)
    {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var na in a.NamedArguments)
        {
            if (na.Key != key) continue;

            var v = na.Value;
            if (v.Type?.TypeKind != TypeKind.Enum) continue;

            if (v.Value == null || v.Type is not INamedTypeSymbol enumType) continue;

            foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
                if (member.HasConstantValue && Equals(member.ConstantValue, v.Value))
                    return member.Name;
        }

        return def;
    }

    private static List<PropInfo> CollectSerializableProperties(INamedTypeSymbol cls)
    {
        var list = new List<PropInfo>();

        foreach (var m in cls.GetMembers())
        {
            if (m is not IPropertySymbol p) continue;
            if (p.DeclaredAccessibility != Accessibility.Public) continue;

            var fa = p.GetAttributes().FirstOrDefault(a => TryGetAttrSimpleName(a) is "Field");
            if (fa is null) continue;

            var order = -1;

            if (!fa.ConstructorArguments.IsDefault)
                order = Convert.ToInt32(fa.ConstructorArguments[0].Value);

            if (order < 0)
                throw new GenError(p.Name, "FieldAttribute does not contain an Order.");

            if (list.Any(x => x.Order == order))
                throw new GenError(p.Name, "The system has detected a duplicate Order.");

            var isVarLen = GetNamed(fa, "IsVarInt", false);
            var writeLength = GetNamed(fa, "WriteLength", true);
            var compressed = GetNamed(fa, "Compressed", false);
            var asDataRef = GetNamed(fa, "AsDataRef", false);
            var presenceBool = GetNamed(fa, "PresenceBool", false);
            var rawLength = GetNamed(fa, "RawLength", -1);
            var addNumber = GetNamed(fa, "AddNumber", 0);
            var calculateSecondsLeft = GetNamed(fa, "CalculateSecondsLeft", false);
            var calculateSecondsPassed = GetNamed(fa, "CalculateSecondsPassed", false);
            var lastOnlineTime = GetNamed(fa, "LastOnlineTime", false);
            var useCustomContract = GetNamed(fa, "UseCustomContract", false);
            var duplicateAfterOrders = GetNamed(fa, "DuplicateAfterOrders", Array.Empty<int>());
            var baseMethodBefore = GetNamed(fa, "BaseMethodBefore", false);
            var baseMethodAfter = GetNamed(fa, "BaseMethodAfter", false);
            var countIsI32 = GetNamed(fa, "CountIsI32", false);

            var kind = GetEnumName(fa, "Type", "Auto") switch
            {
                "I8" => FieldKind.I8,
                "U8" => FieldKind.U8,
                "I16" => FieldKind.I16,
                "U16" => FieldKind.U16,
                "I32" => FieldKind.I32,
                "U32" => FieldKind.U32,
                "I64" => FieldKind.I64,
                "U64" => FieldKind.U64,
                "I128" => FieldKind.I128,
                "U128" => FieldKind.U128,
                "VInt32" => FieldKind.VInt32,
                "VInt64" => FieldKind.VInt64,
                "Bool" => FieldKind.Bool,
                "Str" => FieldKind.Str,
                "CStr" => FieldKind.CStr,
                "Bytes" => FieldKind.Bytes,
                "BytesRaw" => FieldKind.BytesRaw,
                "DataRef" => FieldKind.DataRef,
                "Obj" => FieldKind.Obj,
                "List" => FieldKind.List,
                "Array" => FieldKind.Array,
                "DateTime" => FieldKind.DateTime,
                _ => DetectFieldKind(p.Type, p.Name, asDataRef, writeLength, compressed, isVarLen)
            };

            ValidateFieldFlags(p, kind, isVarLen, writeLength, compressed, asDataRef, rawLength, presenceBool,
                addNumber, calculateSecondsLeft, calculateSecondsPassed, lastOnlineTime, useCustomContract, countIsI32);

            var elem = kind switch
            {
                FieldKind.List when IsList(p.Type, out var e1) || IsByteArray(p.Type, out e1) => e1,
                FieldKind.Array when IsArray(p.Type, out var e2) || IsByteArray(p.Type, out e2) => e2,
                _ => null
            };

            list.Add(new PropInfo(
                p.Name,
                p.Type,
                order,
                kind,
                isVarLen,
                writeLength,
                compressed,
                asDataRef,
                rawLength,
                presenceBool,
                addNumber,
                calculateSecondsLeft,
                calculateSecondsPassed,
                lastOnlineTime,
                useCustomContract,
                duplicateAfterOrders,
                baseMethodBefore,
                baseMethodAfter,
                countIsI32,
                elem
            ));
        }

        foreach (var p in list)
        foreach (var dao in p.DuplicateAfterOrders)
            if (list.All(x => x.Order != dao))
                throw new GenError(p.Name, $"The system has detected unknown Order: {dao} (in DuplicateAfterOrders).");

        return list;
    }

    private static void ValidateFieldFlags(IPropertySymbol p,
        FieldKind kind, bool isVarLen, bool writeLength, bool compressed, bool asDataRef, int rawLength,
        bool presenceBool, int addNumber, bool calculateSecondsLeft, bool calculateSecondsPassed, bool lastOnlineTime,
        bool useCustomContract, bool countIsI32)
    {
        var name = p.Name;

        if (compressed)
            switch (kind)
            {
                case FieldKind.Str or FieldKind.CStr:
                    // OK
                    break;
                case FieldKind.List or FieldKind.Array:
                {
                    var elem = GetElementType(p, kind);

                    if (elem == null || !IsStringType(elem))
                        throw new GenError(name,
                            "Compressed can only be applied to strings or collections of strings.");

                    break;
                }
                default:
                    throw new GenError(name, "Compressed can only be applied to strings or collections of strings.");
            }

        if (isVarLen)
            if (!IsNumeric(kind) && kind != FieldKind.DateTime)
            {
                if (kind is FieldKind.List or FieldKind.Array)
                {
                    var elem = GetElementType(p, kind);

                    if (elem == null || (!IsIntegerIType(elem) && !IsDateTime(elem)))
                        throw new GenError(name,
                            "IsVarInt=true is only valid for numbers, date time and collections of numbers.");
                }
                else
                {
                    throw new GenError(name,
                        "IsVarInt=true is only valid for numbers, date time and collections of numbers.");
                }
            }

        if (asDataRef)
            if (!IsNumeric(kind))
            {
                if (kind is FieldKind.List or FieldKind.Array)
                {
                    var elem = GetElementType(p, kind);

                    if (elem == null || !IsIntegerIType(elem))
                        throw new GenError(name,
                            "AsDataRef=true is only valid for numbers and collections of numbers. (the collection element is not an integer)");
                }
                else
                {
                    throw new GenError(name,
                        $"AsDataRef=true is only valid for numbers and collections of numbers. {kind}");
                }
            }

        if (addNumber != 0)
            if (!IsNumeric(kind) && kind != FieldKind.DateTime)
            {
                if (kind is FieldKind.List or FieldKind.Array)
                {
                    var elem = GetElementType(p, kind);

                    if (elem == null || (!IsIntegerIType(elem) && !IsDateTime(elem)))
                        throw new GenError(name, "AddNumber=n is only valid for numbers and collections of numbers.");
                }
                else
                {
                    throw new GenError(name, "AddNumber=n is only valid for numbers and collections of numbers.");
                }
            }

        if (!writeLength && kind is not FieldKind.Bytes and not FieldKind.BytesRaw and not FieldKind.List
                and not FieldKind.Array)
            throw new GenError(name, "WriteLength=false is only valid for byte[] or collections.");

        if (countIsI32 && kind is not FieldKind.List and not FieldKind.Array)
            throw new GenError(name, "CountIsI32=true is only valid for List and Array.");

        if (rawLength != -1 && kind is not FieldKind.Array and not FieldKind.List and not FieldKind.Bytes
                and not FieldKind.BytesRaw)
            throw new GenError(name,
                "RawLength is only available for BytesRaw and (Bytes, Array, List if WriteLength=false).");

        if (presenceBool && kind is not FieldKind.Obj)
            throw new GenError(name, "PresenceBool is only supported for object properties.");

        if (calculateSecondsLeft)
            if (kind != FieldKind.DateTime)
            {
                if (kind is FieldKind.List or FieldKind.Array)
                {
                    var elem = GetElementType(p, kind);

                    if (elem == null || !IsDateTime(elem))
                        throw new GenError(name,
                            "CalculateSecondsLeft is only valid for DateTime and collections of DateTime.");
                }
                else
                {
                    throw new GenError(name,
                        "CalculateSecondsLeft is only valid for DateTime and collections of DateTime.");
                }
            }

        if (calculateSecondsPassed)
            if (kind != FieldKind.DateTime)
            {
                if (kind is FieldKind.List or FieldKind.Array)
                {
                    var elem = GetElementType(p, kind);

                    if (elem == null || !IsDateTime(elem))
                        throw new GenError(name,
                            "CalculateSecondsPassed is only valid for DateTime and collections of DateTime.");
                }
                else
                {
                    throw new GenError(name,
                        "CalculateSecondsPassed is only valid for DateTime and collections of DateTime.");
                }
            }

        if (lastOnlineTime)
            if (kind != FieldKind.DateTime)
            {
                if (kind is FieldKind.List or FieldKind.Array)
                {
                    var elem = GetElementType(p, kind);

                    if (elem == null || !IsDateTime(elem))
                        throw new GenError(name,
                            "LastOnlineTime is only valid for DateTime and collections of DateTime.");
                }
                else
                {
                    throw new GenError(name,
                        "LastOnlineTime is only valid for DateTime and collections of DateTime.");
                }
            }

        if (calculateSecondsLeft && calculateSecondsPassed)
            throw new GenError(name, "CalculateSecondsLeft and CalculateSecondsPassed cannot be used together.");

        if ((lastOnlineTime && calculateSecondsLeft) || (lastOnlineTime && calculateSecondsPassed))
            throw new GenError(name,
                "LastOnlineTime and (CalculateSecondsLeft or CalculateSecondsPassed) cannot be used together.");

        if (useCustomContract && kind is not FieldKind.Obj and not FieldKind.Array and not FieldKind.List)
            throw new GenError(name,
                "UseCustomContract is only valid for object and collections of objects properties.");

        switch (kind)
        {
            case FieldKind.BytesRaw when !IsByteArray(p.Type, out _):
                throw new GenError(name, "BytesRaw is valid only for byte[].");
            case FieldKind.VInt32 or FieldKind.VInt64 when !IsIntegerIType(p.Type):
                throw new GenError(name, "VInt is only allowed for integer types.");
        }

        return;

        static bool IsStringType(ITypeSymbol t)
        {
            return t.SpecialType == SpecialType.System_String;
        }

        static ITypeSymbol? GetElementType(IPropertySymbol prop, FieldKind k)
        {
            return k switch
            {
                FieldKind.List when prop.Type is INamedTypeSymbol { IsGenericType: true } nt =>
                    nt.TypeArguments.Length > 0
                        ? nt.TypeArguments[0]
                        : null,
                FieldKind.Array when prop.Type is IArrayTypeSymbol at => at.ElementType,
                _ => null
            };
        }
    }

    private static string GenerateForClass(INamedTypeSymbol cls, List<PropInfo> props)
    {
        var ns = cls.ContainingNamespace.IsGlobalNamespace ? null : cls.ContainingNamespace.ToDisplayString();
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using ZOVserver.Shared.TitanRemnants.Streams;");
        sb.AppendLine("using ZOVserver.Shared.TitanRemnants.Streams.Helper;");
        sb.AppendLine();

        if (ns is not null) sb.AppendLine($"namespace {ns}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {cls.Name}");
        sb.AppendLine("    {");

        // Encode
        sb.AppendLine("        public override void Encode(ByteStream stream)");
        sb.AppendLine("        {");
        foreach (var p in props)
        {
            sb.Append(GenerateEncode(p));

            var pap = props.FindAll(x => x.DuplicateAfterOrders.Any(o => o == p.Order));

            foreach (var pp in pap)
                sb.Append(GenerateEncode(pp));
        }

        sb.AppendLine("        }");
        sb.AppendLine();

        // Decode
        sb.AppendLine("        public override void Decode(ByteStream stream)");
        sb.AppendLine("        {");
        foreach (var p in props)
        {
            sb.Append(GenerateDecode(p));

            var pap = props.FindAll(x => x.DuplicateAfterOrders.Any(o => o == p.Order));

            foreach (var pp in pap)
                sb.Append(GenerateDecode(pp));
        }

        sb.AppendLine("        }");

        sb.AppendLine();

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateEncode(PropInfo p)
    {
        var plusText = p.AddNumber != 0 ? $" + {p.AddNumber}" : "";

        var n = p.Name;
        var sb = new StringBuilder();

        if (p.BaseMethodBefore)
        {
            sb.AppendLine("            base.Encode(stream);");
            sb.AppendLine();
        }

        switch (p.Kind)
        {
            case FieldKind.Str:
                sb.AppendLine(p.Compressed
                    ? $"            stream.WriteCompressedString({n});"
                    : $"            stream.WriteString({n});");
                break;

            case FieldKind.CStr:
                sb.AppendLine($"            stream.WriteCompressedString({n});");
                break;

            case FieldKind.Bool:
                sb.AppendLine($"            stream.WriteBoolean({n});");
                break;

            case FieldKind.Bytes:
                if (p.WriteLength)
                    sb.AppendLine($"            stream.WriteBytes({n});");
                else
                    sb.AppendLine($"            stream.WriteBytesWithoutLength({n});");
                break;

            case FieldKind.BytesRaw:
                sb.AppendLine($"            stream.WriteBytesWithoutLength({n});");
                break;

            case FieldKind.DataRef:
                sb.AppendLine($"            ByteStreamHelper.WriteDataReference(stream, {n}{plusText});");
                break;

            case FieldKind.VInt32:
                sb.AppendLine($"            stream.WriteVInt32({n}{plusText});");
                break;

            case FieldKind.VInt64:
                sb.AppendLine($"            stream.WriteVInt64({n}{plusText});");
                break;

            case FieldKind.I8: sb.AppendLine($"            stream.WriteI8({n}{plusText});"); break;
            case FieldKind.U8: sb.AppendLine($"            stream.WriteU8({n}{plusText});"); break;
            case FieldKind.I16: sb.AppendLine($"            stream.WriteI16({n}{plusText});"); break;
            case FieldKind.U16: sb.AppendLine($"            stream.WriteU16({n}{plusText});"); break;
            case FieldKind.I32: sb.AppendLine($"            stream.WriteI32({n}{plusText});"); break;
            case FieldKind.U32: sb.AppendLine($"            stream.WriteU32({n}{plusText});"); break;
            case FieldKind.I64: sb.AppendLine($"            stream.WriteI64({n}{plusText});"); break;
            case FieldKind.U64: sb.AppendLine($"            stream.WriteU64({n}{plusText});"); break;
            case FieldKind.I128: sb.AppendLine($"            stream.WriteI128({n}{plusText});"); break;
            case FieldKind.U128: sb.AppendLine($"            stream.WriteU128({n}{plusText});"); break;

            case FieldKind.DateTime:
                string seconds;

                if (p.CalculateSecondsLeft)
                    seconds = $"Math.Max(0, ({n} - DateTime.UtcNow).TotalSeconds)";
                else if (p.CalculateSecondsPassed)
                    seconds = $"Math.Max(0, (DateTime.UtcNow - {n}).TotalSeconds)";
                else if (p.LastOnlineTime)
                    seconds = $"Math.Max(0, {n} == default ? 0 : (DateTime.UtcNow - {n}).TotalSeconds)";
                else
                    seconds = $"new DateTimeOffset({n}).ToUnixTimeSeconds()";

                if (p.IsVarInt)
                    sb.AppendLine($"            stream.WriteVInt32((int){seconds}{plusText});");
                else
                    sb.AppendLine($"            stream.WriteI32((int){seconds}{plusText});");

                break;

            case FieldKind.Obj:
                sb.AppendLine();

                if (p.PresenceBool)
                {
                    sb.AppendLine($"            stream.WriteBoolean({n} != null);");
                    sb.AppendLine($"            if ({n} != null)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                {n}.{(p.UseCustomContract ? "Custom" : "")}Encode(stream);");
                    sb.AppendLine("            }");
                }
                else
                {
                    sb.AppendLine($"            {n}?.{(p.UseCustomContract ? "Custom" : "")}Encode(stream);");
                }

                sb.AppendLine();
                break;

            case FieldKind.List:
                sb.Append(EncodeCollection(false, p, plusText));
                break;

            case FieldKind.Array:
                sb.Append(EncodeCollection(true, p, plusText));
                break;

            default:
                throw new ArgumentOutOfRangeException(p.Kind.ToString());
        }

        if (p.BaseMethodAfter)
        {
            sb.AppendLine();
            sb.AppendLine("            base.Encode(stream);");
        }

        return sb.ToString();
    }

    private static string EncodeCollection(bool isArray, PropInfo p, string plusText)
    {
        var n = p.Name;
        var elem = p.ElementType;

        if (elem == null)
            throw new GenError(p.Name, "The element type is not specified");

        var countExpr = isArray ? $"{n}?.Length ?? 0" : $"{n}?.Count ?? 0";
        var foreachDecl = isArray
            ? $"for (int i = 0; i < {n}.Length; i++) {{\n                    var item = {n}[i];"
            : $"foreach (var item in {n})\n                {{";

        var sb = new StringBuilder();
        sb.AppendLine();

        if (p.WriteLength)
        {
            if (!p.CountIsI32)
                sb.AppendLine($"            stream.WriteVInt32({countExpr});");
            else
                sb.AppendLine($"            stream.WriteI32({countExpr});");
        }

        sb.AppendLine($"            if ({n} != null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                {foreachDecl}");

        if (IsIntegerIType(elem))
        {
            if (p.AsDataRef)
            {
                sb.AppendLine($"                    ByteStreamHelper.WriteDataReference(stream, item{plusText});");
            }
            else if (p.IsVarInt)
            {
                var v = elem.SpecialType is SpecialType.System_Int64 or SpecialType.System_UInt64
                    ? "WriteVInt64"
                    : "WriteVInt32";

                sb.AppendLine($"                    stream.{v}(item{plusText});");
            }
            else
            {
                sb.AppendLine("                    " + PrimitiveWriteForElement($"item{plusText}", elem));
            }
        }
        else if (IsString(elem))
        {
            sb.AppendLine(p.Compressed
                ? "                    stream.WriteCompressedString(item);"
                : "                    stream.WriteString(item);");
        }
        else if (IsByteArray(elem, out _))
        {
            sb.AppendLine(p.WriteLength
                ? "                    stream.WriteBytes(item);"
                : "                    stream.WriteBytesWithoutLength(item);");
        }
        else if (IsDateTime(elem))
        {
            string seconds;

            if (p.CalculateSecondsLeft)
                seconds = "Math.Max(0, (item - DateTime.UtcNow).TotalSeconds)";
            else if (p.CalculateSecondsPassed)
                seconds = "Math.Max(0, (DateTime.UtcNow - item).TotalSeconds)";
            else if (p.LastOnlineTime)
                seconds = "Math.Max(0, item == default ? 0 : (DateTime.UtcNow - item).TotalSeconds)";
            else
                seconds = "new DateTimeOffset(item).ToUnixTimeSeconds()";

            if (p.IsVarInt)
                sb.AppendLine($"                    stream.WriteVInt32((int){seconds}{plusText});");
            else
                sb.AppendLine($"                    stream.WriteI32((int){seconds}{plusText});");
        }
        else
        {
            sb.AppendLine($"                    item?.{(p.UseCustomContract ? "Custom" : "")}Encode(stream);");
        }

        sb.AppendLine("                }");
        sb.AppendLine("            }");

        sb.AppendLine();

        return sb.ToString();
    }

    private static string PrimitiveWriteForElement(string varName, ITypeSymbol t)
    {
        return t.SpecialType switch
        {
            SpecialType.System_SByte => $"stream.WriteI8({varName});",
            SpecialType.System_Byte => $"stream.WriteU8({varName});",
            SpecialType.System_Int16 => $"stream.WriteI16({varName});",
            SpecialType.System_UInt16 => $"stream.WriteU16({varName});",
            SpecialType.System_Int32 => $"stream.WriteI32({varName});",
            SpecialType.System_UInt32 => $"stream.WriteU32({varName});",
            SpecialType.System_Int64 => $"stream.WriteI64({varName});",
            SpecialType.System_UInt64 => $"stream.WriteU64({varName});",
            _ => t.ToDisplayString() switch
            {
                "System.Int128" => $"stream.WriteI128({varName});",
                "System.UInt128" => $"stream.WriteU128({varName});",
                _ => $"/* unsupported numeric '{t.ToDisplayString()}' */"
            }
        };
    }

    private static string GenerateDecode(PropInfo p)
    {
        var plusText = p.AddNumber != 0 ? $" - {p.AddNumber}" : "";

        var n = p.Name;
        var sb = new StringBuilder();

        if (p.BaseMethodBefore)
        {
            sb.AppendLine("            base.Decode(stream);");
            sb.AppendLine();
        }

        switch (p.Kind)
        {
            case FieldKind.Str:
                sb.AppendLine(p.Compressed
                    ? $"            {n} = stream.ReadCompressedString();"
                    : $"            {n} = stream.ReadString();");
                break;

            case FieldKind.CStr:
                sb.AppendLine($"            {n} = stream.ReadCompressedString();");
                break;

            case FieldKind.Bool:
                sb.AppendLine($"            {n} = stream.ReadBoolean();");
                break;

            case FieldKind.Bytes:
                if (p.WriteLength)
                    sb.AppendLine($"            {n} = stream.ReadBytes().ToArray();");
                else
                    sb.AppendLine($"            {n} = stream.ReadBytesWithoutLength({p.RawLength}).ToArray();");
                break;

            case FieldKind.BytesRaw:
                sb.AppendLine($"            {n} = stream.ReadBytesWithoutLength({p.RawLength}).ToArray();");
                break;

            case FieldKind.DataRef:
                sb.AppendLine($"            {n} = ByteStreamHelper.ReadDataReference(stream){plusText};");
                break;

            case FieldKind.VInt32:
                sb.AppendLine($"            {n} = stream.ReadVInt32(){plusText};");
                break;

            case FieldKind.VInt64:
                sb.AppendLine($"            {n} = stream.ReadVInt64(){plusText};");
                break;

            case FieldKind.I8: sb.AppendLine($"            {n} = stream.ReadI8(){plusText};"); break;
            case FieldKind.U8: sb.AppendLine($"            {n} = stream.ReadU8(){plusText};"); break;
            case FieldKind.I16: sb.AppendLine($"            {n} = stream.ReadI16(){plusText};"); break;
            case FieldKind.U16: sb.AppendLine($"            {n} = stream.ReadU16(){plusText};"); break;
            case FieldKind.I32: sb.AppendLine($"            {n} = stream.ReadI32(){plusText};"); break;
            case FieldKind.U32: sb.AppendLine($"            {n} = stream.ReadU32(){plusText};"); break;
            case FieldKind.I64: sb.AppendLine($"            {n} = stream.ReadI64(){plusText};"); break;
            case FieldKind.U64: sb.AppendLine($"            {n} = stream.ReadU64(){plusText};"); break;
            case FieldKind.I128: sb.AppendLine($"            {n} = stream.ReadI128(){plusText};"); break;
            case FieldKind.U128: sb.AppendLine($"            {n} = stream.ReadU128(){plusText};"); break;

            case FieldKind.DateTime:
                var secondsVar = p.IsVarInt ? $"stream.ReadVInt32(){plusText}" : $"stream.ReadI32(){plusText}";

                if (p.CalculateSecondsLeft)
                    sb.AppendLine($"            {n} = DateTime.UtcNow.AddSeconds({secondsVar});");
                else if (p.CalculateSecondsPassed || p.LastOnlineTime)
                    sb.AppendLine($"            {n} = DateTime.UtcNow.AddSeconds(-{secondsVar});");
                else
                    sb.AppendLine($"            {n} = DateTimeOffset.FromUnixTimeSeconds({secondsVar}).UtcDateTime;");

                break;

            case FieldKind.Obj:
                sb.AppendLine("");

                var t = p.Type;

                if (p.PresenceBool)
                {
                    sb.AppendLine("            if (stream.ReadBoolean())");
                    sb.AppendLine("            {");

                    if (!CanConstruct(t, out var typeName))
                        return ConstructError(n, t);

                    if (typeName.EndsWith('?'))
                        typeName = typeName[..^1];

                    sb.AppendLine($"                {n} ??= new {typeName}();");
                    sb.AppendLine($"                {n}.{(p.UseCustomContract ? "Custom" : "")}Decode(stream);");
                    sb.AppendLine("            }");
                }
                else
                {
                    if (!CanConstruct(t, out var typeName))
                        return ConstructError(n, t);

                    if (typeName.EndsWith('?'))
                        typeName = typeName[..^1];

                    sb.AppendLine($"            {n} ??= new {typeName}();");
                    sb.AppendLine($"            {n}.{(p.UseCustomContract ? "Custom" : "")}Decode(stream);");
                }

                sb.AppendLine("");
                break;

            case FieldKind.List:
                sb.Append(DecodeCollection(false, p, plusText));
                break;

            case FieldKind.Array:
                sb.Append(DecodeCollection(true, p, plusText));
                break;

            default:
                throw new ArgumentOutOfRangeException(p.Kind.ToString());
        }

        if (p.BaseMethodAfter)
        {
            sb.AppendLine();
            sb.AppendLine("            base.Decode(stream);");
        }

        return sb.ToString();
    }

    private static string DecodeCollection(bool isArray, PropInfo p, string plusText)
    {
        var n = p.Name;
        var elem = p.ElementType!;
        var elemName = elem.ToDisplayString();
        var lenVar = $"{Camel(n)}Len";

        var sb = new StringBuilder();
        sb.AppendLine();

        if (p.WriteLength)
        {
            if (!p.CountIsI32)
                sb.AppendLine($"            var {lenVar} = stream.ReadVInt32();");
            else
                sb.AppendLine($"            var {lenVar} = stream.ReadI32();");
        }
        else
        {
            sb.AppendLine($"            var {lenVar} = {p.RawLength};");
        }

        if (isArray)
            sb.AppendLine($"            {n} = new {elemName}[{lenVar}];");
        else
            sb.AppendLine($"            {n} = new List<{elemName}>({lenVar});");

        sb.AppendLine($"            for (int i = 0; i < {lenVar}; i++)");
        sb.AppendLine("            {");

        if (IsIntegerIType(elem))
        {
            if (p.AsDataRef)
            {
                sb.AppendLine($"                var item = ByteStreamHelper.ReadDataReference(stream){plusText};");
            }
            else if (p.IsVarInt)
            {
                var m = elem.SpecialType is SpecialType.System_Int64 or SpecialType.System_UInt64
                    ? "ReadVInt64"
                    : "ReadVInt32";
                sb.AppendLine($"                var item = stream.{m}(){plusText};");
            }
            else
            {
                sb.AppendLine("                " + PrimitiveReadForElement("item", elem, plusText));
            }
        }
        else if (IsString(elem))
        {
            sb.AppendLine(p.Compressed
                ? "                var item = stream.ReadCompressedString();"
                : "                var item = stream.ReadString();");
        }
        else if (IsByteArray(elem, out _))
        {
            if (p.WriteLength)
                sb.AppendLine("                var item = stream.ReadBytes().ToArray();");
            else
                sb.AppendLine($"                var item = stream.ReadBytesWithoutLength({p.RawLength}).ToArray();");
        }
        else if (IsDateTime(elem))
        {
            var secondsVar = p.IsVarInt ? $"stream.ReadVInt32(){plusText}" : $"stream.ReadI32(){plusText}";

            if (p.CalculateSecondsLeft)
                sb.AppendLine($"                var item = DateTime.UtcNow.AddSeconds({secondsVar});");
            else if (p.CalculateSecondsPassed || p.LastOnlineTime)
                sb.AppendLine($"                var item = DateTime.UtcNow.AddSeconds(-{secondsVar});");
            else
                sb.AppendLine(
                    $"                var item = DateTimeOffset.FromUnixTimeSeconds({secondsVar}).UtcDateTime;");
        }
        else
        {
            if (!CanConstruct(elem, out var typeName))
                return ConstructError(n + "[i]", elem);

            if (typeName.EndsWith('?'))
                typeName = typeName[..^1];

            sb.AppendLine($"                var item = new {typeName}();");
            sb.AppendLine($"                item.{(p.UseCustomContract ? "Custom" : "")}Decode(stream);");
        }

        if (isArray)
            sb.AppendLine($"                {n}[i] = item;");
        else
            sb.AppendLine($"                {n}.Add(item);");

        sb.AppendLine("            }");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string PrimitiveReadForElement(string varName, ITypeSymbol t, string plusText)
    {
        return t.SpecialType switch
        {
            SpecialType.System_SByte => $"var {varName} = stream.ReadI8(){plusText};",
            SpecialType.System_Byte => $"var {varName} = stream.ReadU8(){plusText};",
            SpecialType.System_Int16 => $"var {varName} = stream.ReadI16(){plusText};",
            SpecialType.System_UInt16 => $"var {varName} = stream.ReadU16(){plusText};",
            SpecialType.System_Int32 => $"var {varName} = stream.ReadI32(){plusText};",
            SpecialType.System_UInt32 => $"var {varName} = stream.ReadU32(){plusText};",
            SpecialType.System_Int64 => $"var {varName} = stream.ReadI64(){plusText};",
            SpecialType.System_UInt64 => $"var {varName} = stream.ReadU64(){plusText};",
            _ => t.ToDisplayString() switch
            {
                "System.Int128" => $"var {varName} = stream.ReadI128(){plusText};",
                "System.UInt128" => $"var {varName} = stream.ReadU128(){plusText};",
                _ =>
                    $"/* unsupported numeric '{t.ToDisplayString()}' */ var {varName} = default({t.ToDisplayString()});"
            }
        };
    }

    private static bool CanConstruct(ITypeSymbol t, out string typeName)
    {
        typeName = t.ToDisplayString();

        if (t.TypeKind is TypeKind.Interface)
            return false;

        if (t is not INamedTypeSymbol nt) return true;

        return nt.InstanceConstructors.Any(c =>
            c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
    }

    private static string ConstructError(string prop, ITypeSymbol t)
    {
        return
            $"/* Cannot create '{t.ToDisplayString()}' for '{prop}'. Add a specific type/ctor. */\n";
    }

    private static string Camel(string s)
    {
        return string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
    }

    private sealed record PropInfo(
        string Name,
        ITypeSymbol Type,
        int Order,
        FieldKind Kind,
        bool IsVarInt,
        bool WriteLength,
        bool Compressed,
        bool AsDataRef,
        int RawLength,
        bool PresenceBool,
        int AddNumber,
        bool CalculateSecondsLeft,
        bool CalculateSecondsPassed,
        bool LastOnlineTime,
        bool UseCustomContract,
        int[] DuplicateAfterOrders,
        bool BaseMethodBefore,
        bool BaseMethodAfter,
        bool CountIsI32,
        ITypeSymbol? ElementType
    );

    private enum FieldKind
    {
        I8,
        U8,
        I16,
        U16,
        I32,
        U32,
        I64,
        U64,
        I128,
        U128,
        VInt32,
        VInt64,
        Bool,
        Str,
        CStr,
        Bytes,
        BytesRaw,
        DataRef,
        Obj,
        List,
        Array,
        DateTime
    }

    private sealed class GenError(string prop, string msg) : Exception(msg)
    {
        public string PropertyName { get; } = prop;
    }
}