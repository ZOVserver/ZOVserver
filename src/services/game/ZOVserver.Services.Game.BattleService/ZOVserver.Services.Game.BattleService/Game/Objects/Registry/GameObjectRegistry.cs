using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ZOVserver.Services.Game.BattleService.Game.Objects.Interfaces;
using ZOVserver.Services.Game.BattleService.Game.Objects.Registry.Id;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Streams.Helper;

namespace ZOVserver.Services.Game.BattleService.Game.Objects.Registry;

public struct GameObjectRegistry<T>(
    LogicBattleModeServer battleMode,
    GameObjectTypeHelperTable type,
    RegistryRunningId registryRunningId,
    int capacity) :
    IDisposable where T : struct, IGameObject
{
    private T[]? _items = ArrayPool<T>.Shared.Rent(capacity);

    private readonly Dictionary<int, int> _idMap = new(capacity);

    private readonly int _baseId = ((int)type + 1) * 1_000_000;

    public Span<T> All => _items.AsSpan(0, Count);
    public int Count { get; private set; }

    /// <returns>The generated Object ID, or <c>-1</c> if the object is invalid, or <c>-2</c> if the registry is disposed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(in T data)
    {
        if (_items == null)
            return -2;

        if (!data.IsDataInitialized || data.ObjectType != type)
            return -1;

        var id = _baseId + registryRunningId.GetNextId();

        if (Count == _items.Length)
            Grow();

        var index = Count++;

        ref var unit = ref _items[index];
        unit = data;
        unit.Id = id;

        _idMap[id] = index;

        unit.OnAdd(battleMode);

        return id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Remove(int id)
    {
        if (_items == null)
            return;

        if (!_idMap.Remove(id, out var index))
            return;

        _items[index].OnRemove();

        Count--;

        if (index < Count)
        {
            _items[index] = _items[Count];
            _idMap[_items[index].Id] = index;
        }

        _items[Count] = default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetById(int id)
    {
        if (_items == null)
            return ref Unsafe.NullRef<T>();

        ref var index = ref CollectionsMarshal.GetValueRefOrNullRef(_idMap, id);

        if (Unsafe.IsNullRef(ref index))
            return ref Unsafe.NullRef<T>();

        return ref _items[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sweep()
    {
        if (_items == null)
            return;

        var count = Count;

        if (count == 0)
            return;

        ref var ptr = ref MemoryMarshal.GetArrayDataReference(_items);

        for (var i = count - 1; i >= 0; i--)
        {
            ref var unit = ref Unsafe.Add(ref ptr, i);

            if (unit.ShouldRemove)
                Remove(unit.Id);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Tick()
    {
        if (_items == null)
            return;

        var count = Count;

        if (count == 0)
            return;

        ref var ptr = ref MemoryMarshal.GetArrayDataReference(_items);

        for (var i = 0; i < count; i++)
        {
            ref var unit = ref Unsafe.Add(ref ptr, i);

            unit.Tick();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetObjectsCount(int myIndex)
    {
        if (_items == null)
            return 0;

        var count = Count;

        if (count == 0)
            return 0;

        var c = 0u;

        ref var ptr = ref MemoryMarshal.GetArrayDataReference(_items);

        for (var i = 0; i < count; i++)
        {
            ref var unit = ref Unsafe.Add(ref ptr, i);

            if (unit.ShouldEncodeFor(myIndex))
                c++;
        }

        return c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EncodeDataGlobalIds(ref BitStream b, int myIndex)
    {
        if (_items == null)
            return 0;

        var count = Count;

        if (count == 0)
            return 0;

        var encodes = 0;

        ref var ptr = ref MemoryMarshal.GetArrayDataReference(_items);

        for (var i = 0; i < count; i++)
        {
            ref var unit = ref Unsafe.Add(ref ptr, i);

            // ReSharper disable once InvertIf
            if (unit.ShouldEncodeFor(myIndex))
            {
                BitStreamHelper.WriteDataReference(ref b, unit.ObjectData.GlobalId);
                encodes++;
            }
        }

        return encodes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EncodeObjectIds(ref BitStream b, int myIndex)
    {
        if (_items == null)
            return 0;

        var count = Count;

        if (count == 0)
            return 0;

        var encodes = 0;

        ref var ptr = ref MemoryMarshal.GetArrayDataReference(_items);

        for (var i = 0; i < count; i++)
        {
            ref var unit = ref Unsafe.Add(ref ptr, i);

            // ReSharper disable once InvertIf
            if (unit.ShouldEncodeFor(myIndex))
            {
                BitStreamHelper.WriteObjectRunningId(ref b, unit.Id);
                encodes++;
            }
        }

        return encodes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EncodeObjects(ref BitStream b, int myIndex)
    {
        if (_items == null)
            return 0;

        var count = Count;

        if (count == 0)
            return 0;

        var encodes = 0;

        ref var ptr = ref MemoryMarshal.GetArrayDataReference(_items);

        for (var i = 0; i < count; i++)
        {
            ref var unit = ref Unsafe.Add(ref ptr, i);

            // ReSharper disable once InvertIf
            if (unit.ShouldEncodeFor(myIndex))
            {
                unit.Encode(ref b, myIndex);
                encodes++;
            }
        }

        return encodes;
    }

    private void Grow()
    {
        if (_items == null)
            return;

        var newArr = ArrayPool<T>.Shared.Rent(_items.Length * 2);
        _items.AsSpan(0, Count).CopyTo(newArr);

        ArrayPool<T>.Shared.Return(_items);
        _items = newArr;
    }

    public void Dispose()
    {
        if (_items == null)
            return;

        ArrayPool<T>.Shared.Return(_items, true);
        _items = null;

        _idMap.Clear();
        Count = 0;
    }
}