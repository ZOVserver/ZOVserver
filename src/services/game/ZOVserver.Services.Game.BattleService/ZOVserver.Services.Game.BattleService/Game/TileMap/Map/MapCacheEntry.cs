using System.Collections.Frozen;
using ZOVserver.Services.Game.BattleService.Game.TileMap.Tile;

namespace ZOVserver.Services.Game.BattleService.Game.TileMap.Map;

public sealed class MapCacheEntry
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    public required LogicTile[] HorizontalTiles { get; init; } // (Y*W+X)
    public required LogicTile[] VerticalTiles { get; init; } // (X*H+Y)
    public required FrozenDictionary<char, LogicTile[]> DigitTiles { get; init; } // (0-9)
    public required int[] DestructibleVerticalIndices { get; init; } // (GetIsDestructible() == true)
}