using ZOVserver.Services.Game.BattleService.Game.TileMap.Tile;

namespace ZOVserver.Services.Game.BattleService.Game.TileMap.Interfaces;

public interface ITileAction
{
    void Execute(LogicTileMap tileMap, ref readonly LogicTile tile);
}