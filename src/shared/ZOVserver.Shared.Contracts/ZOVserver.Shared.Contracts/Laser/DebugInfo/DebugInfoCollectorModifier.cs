namespace ZOVserver.Shared.Contracts.Laser.DebugInfo;

public static class DebugInfoCollectorModifier
{
    public static void UpdateYCollections(string filePath)
    {
        var fileContent = File.ReadAllText(filePath);

        var packetCollectorY = DebugInfoCollector.PacketCollectorY.ToDictionary();
        var commandCollectorY = DebugInfoCollector.CommandCollectorY.ToDictionary();

        Console.WriteLine($"PacketCollectorY count: {packetCollectorY.Count}");
        Console.WriteLine($"CommandCollectorY count: {commandCollectorY.Count}");

        var filteredPacketY = FilterDictionary(packetCollectorY, fileContent);
        var filteredCommandY = FilterDictionary(commandCollectorY, fileContent);

        Console.WriteLine($"PacketCollectorYFromV24 count: {filteredPacketY.Count}");
        Console.WriteLine($"CommandCollectorYFromV24 count: {filteredCommandY.Count}");

        GenerateUpdatedClass(filteredPacketY, filteredCommandY);
    }

    private static Dictionary<TKey, TValue> FilterDictionary<TKey, TValue>(
        Dictionary<TKey, TValue> dictionary, string fileContent) where TKey : notnull
    {
        return dictionary
            .Where(kvp => fileContent.Contains($"return {kvp.Key.ToString()};"))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static void GenerateUpdatedClass(
        Dictionary<int, (string, int)> packetCollectorY,
        Dictionary<int, string> commandCollectorY)
    {
        Console.WriteLine("        PacketCollectorYFromV24 = new Dictionary<int, (string, int)>");
        Console.WriteLine("        {");
        foreach (var kvp in packetCollectorY.OrderBy(x => x.Key))
            Console.WriteLine($"            {{ {kvp.Key}, (\"{kvp.Value.Item1}\", {kvp.Value.Item2}) }},");
        Console.WriteLine("        }.ToFrozenDictionary();");

        Console.WriteLine();

        Console.WriteLine("        CommandCollectorYFromV24 = new Dictionary<int, string>");
        Console.WriteLine("        {");
        foreach (var kvp in commandCollectorY.OrderBy(x => x.Key))
            Console.WriteLine($"            {{ {kvp.Key}, \"{kvp.Value}\" }},");
        Console.WriteLine("        }.ToFrozenDictionary();");
    }
}