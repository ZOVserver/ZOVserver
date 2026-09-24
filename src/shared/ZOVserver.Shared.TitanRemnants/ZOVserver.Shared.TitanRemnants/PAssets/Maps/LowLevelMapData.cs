namespace ZOVserver.Shared.TitanRemnants.PAssets.Maps;

internal sealed class LowLevelMapData
{
    public (int, int) WidthAndHeight = (0, 0);
    public string GroupName { get; init; } = null!;

    public List<string> DataLines { get; } = [];
    public string FullData { get; set; } = null!;
    public string Metadata { get; set; } = null!;
}