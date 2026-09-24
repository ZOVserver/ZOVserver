namespace ZOVserver.Services.Game.BattleService.Game.Objects.Registry.Id;

public sealed class RegistryRunningId(int maxObjects)
{
    private readonly int _max = maxObjects - 1;

    private int _id;

    public int GetNextId()
    {
        return _id >= _max ? throw new Exception("Max RunningId reached") : _id++;
    }

    public void Reset()
    {
        _id = 0;
    }
}