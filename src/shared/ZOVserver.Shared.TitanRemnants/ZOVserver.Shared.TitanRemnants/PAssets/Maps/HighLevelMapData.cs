namespace ZOVserver.Shared.TitanRemnants.PAssets.Maps;

public sealed class HighLevelMapData(string mapName, string mapData, string metadata, int width, int height)
{
    public string MapName { get; } = mapName ?? throw new ArgumentNullException(nameof(mapName));

    public string MapData { get; } = mapData ?? throw new ArgumentNullException(nameof(mapData));
    public string Metadata { get; } = metadata ?? throw new ArgumentNullException(nameof(metadata));

    public uint Width { get; } = (uint)width;
    public uint Height { get; } = (uint)height;

    public override string ToString()
    {
        return $"HighLevelMapData =>\n" +
               $"MapName = {MapName}\n" +
               $"MapData = {MapData}\n" +
               $"Metadata = {Metadata}\n" +
               $"Width = {Width}, " +
               $"Height = {Height}";
    }
}