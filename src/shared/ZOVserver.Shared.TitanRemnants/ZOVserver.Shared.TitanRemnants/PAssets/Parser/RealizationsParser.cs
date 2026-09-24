using System.Text;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Parser;

public class RealizationsParser(string inPath)
{
    private const string T16Methods = """
                                          public bool IsDecoy()
                                          {
                                              return TypeInCsv == "Minion_Mirage";
                                          }

                                          public bool IsBoss()
                                          {
                                              return TypeInCsv is "Npc_Boss" or "Npc_Boss_TownCrush";
                                          }

                                          public bool IsTrain()
                                          {
                                              return TypeInCsv == "Train";
                                          }

                                          public bool IsRoboWars()
                                          {
                                              return TypeInCsv == "RoboWars";
                                          }

                                          public bool IsBase()
                                          {
                                              return TypeInCsv == "Pvp_Base";
                                          }

                                          public bool IsLootBox()
                                          {
                                              return TypeInCsv == "LootBox";
                                          }

                                          public bool IsTrainingDummy()
                                          {
                                              return TypeInCsv == "Minion_Building_charges_ulti";
                                          }

                                          public bool IsTownCrushBoss()
                                          {
                                              return TypeInCsv == "Npc_Boss_TownCrush";
                                          }

                                          public bool IsCarryable()
                                          {
                                              return TypeInCsv == "Carryable";
                                          }

                                          public bool IsHero()
                                          {
                                              return TypeInCsv == "Hero";
                                          }

                                          public bool IsPet()
                                          {
                                              return Pet != null!;
                                          }

                                          public bool IsMinionDuplicate()
                                          {
                                              return TypeInCsv == "Minion_Duplicate";
                                          }

                                          public bool IsMinionDog()
                                          {
                                              return TypeInCsv == "Minion_Dog";
                                          }

                                          public bool IsPayload()
                                          {
                                              return TypeInCsv == "Payload";
                                          }

                                          public bool HasAutoAttack()
                                          {
                                              return AutoAttackDamage > 0;
                                          }

                                          public bool HasVeryMuchHitPoints()
                                          {
                                              return Hitpoints > 5799;
                                          }
                                      """;

    private static readonly Dictionary<string, string> ExclusionDictionary = new()
    {
        { "TID", "Tid" },
        { "SWF", "Swf" },
        { "SCW", "Scw" },
        { "SDK", "Sdk" },
        { "CN", "Cn" },
        { "RTL", "Rtl" },
        { "MS", "Ms" },
        { "VO", "Vo" },
        { "SD", "Sd" },
        { "MB", "Mb" },
        { "ID", "Id" },
        { "XP", "Xp" },
        { "SCID", "Scid" },
        { "VFX", "Vfx" },
        { "ELO", "Elo" }
    };

    private readonly StringBuilder _stringBuilder = new();

    public void ParseCsv(string outPath, string namespaceText, string[] lines, params string[] usings)
    {
        var fName = DataTablesInfo.GetTableFilenameByPath(inPath);
        var fileName = DataTablesInfo.GetTableNameByCsvName(fName);
        var tableId = DataTablesInfo.GetTableIdByCsvName(fName);

        if (fName.StartsWith("map"))
            return;

        if (lines.Length < 2)
            throw new Exception($"Error with csv header (invalid lines count -> {lines.Length}).");

        var l0 = lines[0];
        {
            l0 = l0.Trim();
            l0 = l0.Replace(" ", "");

            if (l0.Contains("\","))
                l0 = l0.Replace("\"", "");
        }

        var l1 = lines[1];
        {
            l1 = l1.Trim();
            l1 = l1.Replace(" ", "");
            l1 = l1.ToLower();

            if (l1.Contains("\","))
                l1 = l1.Replace("\"", "");
        }

        var names = l0.Split(',');
        var types = l1.Split(',');

        if (names.Length != types.Length)
            throw new Exception($"Error with csv header (n -> {names.Length} != t -> {types.Length}).");

        lines = lines.Skip(2).ToArray();

        if (lines.Length == 0)
            throw new Exception("Error with csv payload (null payload).");

        var line = lines[0];

        line = line.Trim();
        line = line.Replace(" ", "");

        while (line.Contains(",,"))
            line = line.Replace(",,", ",null,");

        if (line.Contains("\","))
            line = line.Replace("\"", "");

        var lineFragments = line.Split(',');

        if (lineFragments.Length > names.Length)
        {
            Console.WriteLine($"Error with csv payload (lf -> {lineFragments.Length} != n -> {names.Length}).");
            Console.WriteLine(
                $"Type <skip> to ignore the file ({fileName}), or type <close> to terminate the program.");

            var response = Console.ReadLine();

            if (response == null || !response.Contains("skip", StringComparison.CurrentCultureIgnoreCase))
                throw new Exception(
                    $"Error with csv payload (lf -> {lineFragments.Length} != n -> {names.Length}).");

            return;
        }

        if (names.Length <= 15)
            _stringBuilder.AppendLine("using Spectre.Console;");

        foreach (var u in usings)
            _stringBuilder.AppendLine($"using {u};");
        _stringBuilder.AppendLine("");

        _stringBuilder.AppendLine($"namespace {namespaceText};");
        _stringBuilder.AppendLine("");

        _stringBuilder.AppendLine(
            $"// Generated ({tableId}) at {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} ({Guid.NewGuid().ToString()}).");
        _stringBuilder.AppendLine(
            $"public class {fileName}(CsvElement csvElement) : LogicData(csvElement)");

        _stringBuilder.AppendLine("{");

        for (var j = 0; j < lineFragments.Length; j++)
            if (!ParseLineFragment(names[j], types[j], lineFragments[j], (fileName, 0, j)))
                throw new Exception(
                    $"Error with csv line fragment ({lineFragments[j]}, {names[j]}, {types[j]}, {0}, {j})");

        _stringBuilder.AppendLine("");

        if (tableId == 16)
        {
            _stringBuilder.AppendLine(T16Methods);
            _stringBuilder.AppendLine("");
        }

        if (names.Length <= 15)
        {
            _stringBuilder.AppendLine("    public override string ToString()");
            _stringBuilder.AppendLine("    {");
            _stringBuilder.AppendLine("        var table = new Table");
            _stringBuilder.AppendLine("        {");
            _stringBuilder.AppendLine($"            Title = new TableTitle(\"{fileName}\")");
            _stringBuilder.AppendLine("        };");

            _stringBuilder.AppendLine("");

            foreach (var name in names)
                _stringBuilder.AppendLine($"        table.AddColumn(\"[bold blue]{name}[/]\");");

            _stringBuilder.Append("        table.AddRow(");
            for (var j = 0; j < lineFragments.Length; j++)
            {
                if (names[j] == "Name")
                {
                    _stringBuilder.Append("$\"[green]{Name}[/]\"");
                }
                else
                {
                    var propName = GetCsharpPublicName(names[j]);

                    if (GetCsharpType(types[j]) == "string")
                        _stringBuilder.Append($"$\"[green]{{{propName}}}[/]\".Replace(\"\\\"\", \"\")");
                    else
                        _stringBuilder.Append($"$\"[green]{{{propName}}}[/]\"");
                }

                if (j < lineFragments.Length - 1)
                    _stringBuilder.Append(", ");
            }

            _stringBuilder.AppendLine(");");

            _stringBuilder.AppendLine("");

            _stringBuilder.AppendLine("        using var writer = new StringWriter();");
            _stringBuilder.AppendLine(
                "        AnsiConsole.Create(new AnsiConsoleSettings {Out = new AnsiConsoleOutput(writer)}).Write(table);");
            _stringBuilder.AppendLine("        return writer.ToString();");
        }
        else
        {
            var toStringText = new StringBuilder();
            {
                toStringText.AppendLine($"               {fileName} =>");

                for (var j = 0; j < lineFragments.Length; j++)
                {
                    if (names[j] == "Name")
                    {
                        toStringText.AppendLine("                  Name = {Name},");
                        continue;
                    }

                    var propName = GetCsharpPublicName(names[j]);

                    toStringText.AppendLine($"                  {names[j]} = {{{propName}}}" +
                                            (j == lineFragments.Length - 1 ? "" : ","));
                }
            }

            _stringBuilder.AppendLine("    public override string ToString()");
            _stringBuilder.AppendLine("    {");
            _stringBuilder.AppendLine("        return $\"\"\"");
            _stringBuilder.Append(toStringText);
            _stringBuilder.AppendLine("               \"\"\";");
        }

        _stringBuilder.AppendLine("    }");
        _stringBuilder.AppendLine("}");

        var fullPath = Path.Combine(outPath, fileName + ".cs");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, _stringBuilder.ToString(), Encoding.UTF8);
    }

    private bool ParseLineFragment(string v1, string v2, string v3, (string, int, int) v4)
    {
        if (v1 == "Name")
            return true;

        Console.WriteLine($"({v4.Item1}-{v4.Item2}) ->   {v1}: {v2} = {v3}");

        _stringBuilder.AppendLine(
            $"    public {GetCsharpType(v2)} {GetCsharpPublicName(v1)} {{ get; }} = csvElement.{GetCsharpMethodType(v2)}(\"{v1}\");");

        return true;
    }

    private static string GetCsharpType(string csvType)
    {
        return csvType switch
        {
            "int" => "int",
            "float" => "float",
            "string" or "stringarray" => "string",
            "boolean" => "bool",
            _ => throw new ArgumentException($"Invalid csv type -> {csvType}")
        };
    }

    private static string GetCsharpMethodType(string csvType)
    {
        return csvType switch
        {
            "int" => "GetIntValue",
            "float" => "GetFloatValue",
            "string" or "stringarray" => "GetStringValue",
            "boolean" => "GetBoolValue",
            _ => throw new ArgumentException($"Invalid csv type -> {csvType}")
        };
    }

    private static string GetCsharpPublicName(string csvName)
    {
        FixCsvName(ref csvName);

        if (string.IsNullOrEmpty(csvName))
            return csvName;

        var propName = csvName[0].ToString().ToUpper() + csvName[1..];

        if (char.IsDigit(propName[0]))
            propName = "_" + propName;

        if (propName == "Type")
            propName = "TypeInCsv";

        return propName;
    }

    private static void FixCsvName(ref string csvName)
    {
        csvName = ExclusionDictionary.Aggregate(csvName, (current, r) => current.Replace(r.Key, r.Value));
    }
}