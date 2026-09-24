using System.Collections.Frozen;
using ZOVserver.Services.Game.BattleService.Game.TileMap.Tile;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Maps;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.BattleService.Game.TileMap.Map;

public static class LogicMapLoader
{
    private static readonly LogicTileData[] DataByCode;
    private static readonly FrozenDictionary<HighLevelMapData, MapCacheEntry> Cache;

    static LogicMapLoader()
    {
        DataByCode = new LogicTileData[256];
        var cache = new Dictionary<HighLevelMapData, MapCacheEntry>();

        var allTiles = LogicDataTables.GetAllDataByClassId<LogicTileData>(27)!;

        foreach (var tileData in allTiles)
        {
            var code = tileData.TileCode[0];

            if (code < 256)
                DataByCode[code] = tileData;
        }

        var allMaps = LogicDataTables.GetMapsManager(19)!.GetMapsData();

        foreach (var map in allMaps)
        {
            if (map.MapName == "GeneratedRobo")
                continue;

            var w = (int)map.Width;
            var h = (int)map.Height;

            var count = w * h;

            var ht = new LogicTile[count];
            var vt = new LogicTile[count];
            var dt = new Dictionary<char, List<LogicTile>>();

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var index = y * w + x;
                var code = map.MapData[index];

                if (code is >= '0' and <= '9')
                {
                    var tile = new LogicTile(code, null, x, y, index);

                    if (!dt.TryGetValue(code, out var list))
                    {
                        list = [];
                        dt[code] = list;
                    }

                    list.Add(tile);

                    ht[index] = tile;
                }
                else
                {
                    ht[index] = new LogicTile(code, DataByCode[code], x, y, index);
                }
            }

            var vIdx = 0;
            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
                vt[vIdx++] = ht[y * w + x];

            var dtf = dt.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());

            var dvi = new List<int>();

            foreach (ref readonly var tile in vt.AsSpan())
                if (tile.TileData?.IsDestructible == true)
                    dvi.Add(tile.LinearIndex);

            cache[map] = new MapCacheEntry
            {
                Width = w,
                Height = h,
                HorizontalTiles = ht,
                VerticalTiles = vt,
                DigitTiles = dtf,
                DestructibleVerticalIndices = dvi.ToArray()
            };
        }

        Cache = cache.ToFrozenDictionary();
    }

    public static LogicTileData? TileCodeToTileData(char code)
    {
        if (code is >= '0' and <= '9' || code > 255)
            return null;

        return DataByCode[code];
    }

    public static MapCacheEntry GetMapCacheEntry(HighLevelMapData map)
    {
        return Cache[map];
    }

    public static int GetMapCacheEntriesCount()
    {
        return Cache.Count;
    }
}