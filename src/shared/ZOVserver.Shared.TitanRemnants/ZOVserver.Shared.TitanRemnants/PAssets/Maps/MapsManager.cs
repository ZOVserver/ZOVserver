using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Maps;

public class MapsManager
{
    private readonly CsvData _csvData;

    public MapsManager(CsvData csvData)
    {
        if (!csvData.IsMap()) throw new Exception("CsvData must be a Map");
        _csvData = csvData;
    }

    public HighLevelMapData? GetMapData(string mapName)
    {
        return _csvData.GetMapData(mapName);
    }

    public HighLevelMapData[] GetMapsData()
    {
        return _csvData.GetMapsData();
    }
}