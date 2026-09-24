using ZOVserver.Services.Game.BattleService.Game.TileMap.Interfaces;
using ZOVserver.Services.Game.BattleService.Game.TileMap.Tile;

namespace ZOVserver.Services.Game.BattleService.Game.TileMap.Actions;

internal readonly struct DestroyTileAction(bool normalOnly) : ITileAction
{
    public void Execute(LogicTileMap tileMap, ref readonly LogicTile tile)
    {
        var d = tile.TileData;

        if (d == null)
            return;

        if (normalOnly ? d.IsDestructibleNormalWeapon : d.IsDestructible)
            ((ILogicTileMapInternal)tileMap).SetDestroyedInternal(tile.LinearIndex);
    }
}