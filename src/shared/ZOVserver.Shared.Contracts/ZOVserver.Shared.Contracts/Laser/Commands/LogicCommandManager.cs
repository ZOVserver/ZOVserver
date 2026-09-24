using System.Collections.Frozen;
using System.Reflection;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Commands;

public static class LogicCommandManager
{
    private static FrozenDictionary<int, Type> _commands;

    static LogicCommandManager()
    {
        _commands = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(LogicCommand)))
            .Where(t => t.GetConstructors().Any(c => c.GetParameters().Length == 0))
            .ToDictionary(commandTypeof =>
                (Activator.CreateInstance(commandTypeof) as LogicCommand)!.GetCommandType())
            .ToFrozenDictionary();
    }

    public static (int, LogicCommand?) DecodeCommand(ByteStream byteStream, bool alsoUseCustomContract = true)
    {
        var type = byteStream.ReadVInt32();

        if (type == 0)
            return (555041, null); // frida magic (old)

        var logicCommand = CreateCommand(type);

        if (logicCommand == null)
            return (type, null);

        logicCommand.Decode(byteStream);

        if (alsoUseCustomContract)
            logicCommand.CustomDecode(byteStream);

        return (type, logicCommand);
    }

    public static void EncodeCommand(LogicCommand command, ByteStream byteStream, bool alsoUseCustomContract = true)
    {
        byteStream.WriteVInt32(command.GetCommandType());

        command.Encode(byteStream);

        if (alsoUseCustomContract)
            command.CustomEncode(byteStream);
    }

    public static LogicCommand? CreateCommand(int type)
    {
        if (_commands.TryGetValue(type, out var command))
            return Activator.CreateInstance(command) as LogicCommand;

        // Console.WriteLine($"New unknown command handled! Info: Type = {type}; Name = {DebugInfoCollector.CommandCollectorY.GetValueOrDefault(type, "unknown-name")}.");

        return null;
    }

    public static int[] GetAllSavedCommands()
    {
        return _commands.Keys.ToArray();
    }

    public static void Destruct()
    {
        _commands = null!;
    }
}