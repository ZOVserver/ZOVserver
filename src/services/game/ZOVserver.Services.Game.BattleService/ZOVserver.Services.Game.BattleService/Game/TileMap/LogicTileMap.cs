using System.Collections;
using System.Runtime.CompilerServices;
using ZOVserver.Services.Game.BattleService.Game.TileMap.Actions;
using ZOVserver.Services.Game.BattleService.Game.TileMap.Interfaces;
using ZOVserver.Services.Game.BattleService.Game.TileMap.Map;
using ZOVserver.Services.Game.BattleService.Game.TileMap.Tile;
using ZOVserver.Shared.TitanRemnants.PAssets.Maps;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Services.Game.BattleService.Game.TileMap;

public sealed class LogicTileMap : ILogicTileMapInternal
{
    private readonly MapCacheEntry _cache;
    private readonly BitArray _destroyedFlags;

    public LogicTileMap(HighLevelMapData hmap)
    {
        _cache = LogicMapLoader.GetMapCacheEntry(hmap);
        _destroyedFlags = new BitArray(_cache.HorizontalTiles.Length);
    }

    public int Width => _cache.Width;
    public int Height => _cache.Height;

    public int LogicWidth => TileToLogic(_cache.Width);
    public int LogicHeight => TileToLogic(_cache.Height);

    public ref readonly LogicTile this[int x, int y, bool isLogic = false]
    {
        get
        {
            var index = GetTileIndex(x, y, isLogic);

            if (index == -1)
                return ref Unsafe.NullRef<LogicTile>();

            return ref _cache.HorizontalTiles[index];
        }
    }

    void ILogicTileMapInternal.SetDestroyedInternal(int index)
    {
        _destroyedFlags.Set(index, true);
    }

    public ref readonly LogicTile[] GetDigitTiles(char code)
    {
        if (code is >= '0' and <= '9')
            return ref _cache.DigitTiles.GetValueRefOrNullRef(code);

        return ref Unsafe.NullRef<LogicTile[]>();
    }

    public ref readonly LogicTile GetTile(int x, int y, out bool notNullRef, out bool destroyed, bool isLogic = false)
    {
        var index = GetTileIndex(x, y, isLogic);

        if (index == -1)
        {
            notNullRef = false;
            destroyed = false;

            return ref Unsafe.NullRef<LogicTile>();
        }

        notNullRef = true;
        destroyed = _destroyedFlags.Get(index);

        return ref _cache.HorizontalTiles[index];
    }

    private int GetTileIndex(int x, int y, bool isLogic)
    {
        var tx = isLogic ? LogicToTile(x) : x;
        var ty = isLogic ? LogicToTile(y) : y;

        if (tx < 0 || tx >= _cache.Width || ty < 0 || ty >= _cache.Height)
            return -1;

        return ty * _cache.Width + tx;
    }

    public void DestroyTile(int x, int y, bool isLogic = false)
    {
        var index = GetTileIndex(x, y, isLogic);

        if (index != -1)
            _destroyedFlags.Set(index, true);
    }

    public void DestructTile(int x, int y, bool isLogic, bool piercesEnvironment = true)
    {
        ref readonly var tile = ref this[x, y, isLogic];

        if (Unsafe.IsNullRef(in tile))
            return;

        if (tile.TileData is not { IsDestructible: true })
            return;

        if (piercesEnvironment || tile.TileData.IsDestructibleNormalWeapon)
            _destroyedFlags.Set(tile.LinearIndex, true);
    }

    public bool IsTileDestroyed(int x, int y, bool isLogic = false)
    {
        var index = GetTileIndex(x, y, isLogic);

        return index != -1 && _destroyedFlags.Get(index);
    }

    public bool IsPassablePathFinder(int x, int y, bool ignoreWater = false)
    {
        ref readonly var tile = ref GetTile(x, y, out var notNullRef, out var destroyed);

        if (!notNullRef)
            return false;

        if (tile.TileData == null)
            return true;

        if (!ignoreWater && tile.TileCode == 'W')
            return true;

        return !tile.TileData.BlocksMovement || destroyed;
    }

    public void ExecuteInRadius<T>(int lcx, int lcy, int radius, bool isLogic, T action)
        where T : struct, ITileAction
    {
        var cx = isLogic ? lcx : TileToLogic(lcx);
        var cy = isLogic ? lcy : TileToLogic(lcy);

        var rSq = (long)radius * radius;
        var radiusInTiles = radius / 300 + 1;

        var x0 = Math.Max(0, cx / 300 - radiusInTiles);
        var x1 = Math.Min(_cache.Width - 1, cx / 300 + radiusInTiles);

        var y0 = Math.Max(0, cy / 300 - radiusInTiles);
        var y1 = Math.Min(_cache.Height - 1, cy / 300 + radiusInTiles);

        var tiles = _cache.HorizontalTiles;
        var width = _cache.Width;

        for (var y = y0; y <= y1; y++)
        {
            var tileCenterY = y * 300 + 150;
            var dy = tileCenterY - cy;
            var dySq = dy * dy;

            var rowOffset = y * width;

            for (var x = x0; x <= x1; x++)
            {
                var tileCenterX = x * 300 + 150;
                var dx = tileCenterX - cx;

                if (dx * dx + dySq <= rSq)
                    action.Execute(this, ref tiles[rowOffset + x]);
            }
        }
    }

    public void DestroyEnvironment(int cx, int cy, int r, bool isLogic, bool normalOnly)
    {
        ExecuteInRadius(cx, cy, r, isLogic, new DestroyTileAction(normalOnly));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LogicToTile(int logic)
    {
        return logic / 300;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TileToLogic(int tile)
    {
        return 300 * tile + 150;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LogicToTile(float logic)
    {
        return logic / 300;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float TileToLogic(float tile)
    {
        return 300 * tile + 150;
    }

    public void Encode(ref BitStream b)
    {
        var v32 = 0;
        var v31 = 0;
        var v34 = Width - 1;
        var v33 = Height - 1;

        var v35 = Math.Clamp(v32, 0, Width - 1);
        var v36 = Math.Clamp(v31, 0, Height - 1);
        var v37 = Math.Clamp(v34, 0, Width - 1);
        var v38 = Math.Clamp(v33, 0, Height - 1);

        b.WriteBoolean(v35 != v32);
        b.WriteBoolean(v36 != v31);
        b.WriteBoolean(v37 != v34);
        b.WriteBoolean(v38 != v33);

        if (Width >= 22)
        {
            b.WritePositiveIntMax63((uint)v35);
            b.WritePositiveIntMax63((uint)v36);
            b.WritePositiveIntMax63((uint)v37);
        }
        else
        {
            b.WritePositiveIntMax31((uint)v35);
            b.WritePositiveIntMax63((uint)v36);
            b.WritePositiveIntMax31((uint)v37);
        }

        b.WritePositiveIntMax63((uint)v38);

        ReadOnlySpan<int> indices = _cache.DestructibleVerticalIndices.AsSpan();
        var len = indices.Length;

        for (var i = 0; i < len; i++)
            b.WriteBoolean(_destroyedFlags.Get(indices[i]));
    }
}