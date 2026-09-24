using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

namespace ZOVserver.Services.Game.BattleService.Game.TileMap.Tile;

public readonly struct LogicTile
{
    public readonly char TileCode;
    public readonly LogicTileData? TileData;

    public readonly int TileX;
    public readonly int TileY;

    public readonly int LogicX;
    public readonly int LogicY;

    public readonly int LinearIndex;

    public LogicTile(char tileCode, LogicTileData? data, int x, int y, int linearIndex, bool isLogic = false)
    {
        TileCode = tileCode;
        TileData = data;

        if (isLogic)
        {
            TileX = LogicTileMap.LogicToTile(x);
            TileY = LogicTileMap.LogicToTile(y);

            LogicX = x;
            LogicY = y;
        }
        else
        {
            TileX = x;
            TileY = y;

            LogicX = LogicTileMap.TileToLogic(x);
            LogicY = LogicTileMap.TileToLogic(y);
        }

        LinearIndex = linearIndex;
    }
}