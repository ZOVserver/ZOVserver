namespace ZOVserver.Services.Game.BattleService.Network;

public static class BattleServerManager
{
    private static readonly List<UdpBattleServerInstance> Instances = [];
    public static int MinThreads { get; set; } = Environment.ProcessorCount * 2;

    public static void Start(int startPort, int count)
    {
        ThreadPool.SetMinThreads(count * MinThreads, count * MinThreads);

        for (var i = 0; i < count; i++)
        {
            var instance = new UdpBattleServerInstance(startPort + i);
            _ = instance.RunAsync();

            Instances.Add(instance);
        }
    }

    public static UdpBattleServerInstance? GetBestInstance()
    {
        return Instances
            .OrderBy(x => x.ActiveSessionsCount)
            .ThenBy(x => x.CurrentPps)
            .FirstOrDefault();
    }
}