using System.Reflection;
using Cysharp.Threading;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using Orleans.Configuration;
using ZOVserver.RasputinProtect;
using ZOVserver.Services.Game.BattleService.Game.TileMap.Map;
using ZOVserver.Services.Game.BattleService.Network;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.BattleService;

public static class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task Main(string[] args)
    {
        LogoWriter.ShowLogo();

        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;

            var battleSettings = configuration.GetSection("BattleSettings");

            BattleServerManager.MinThreads = battleSettings.GetValue<int?>("MinThreads") ?? Environment.ProcessorCount;


            UdpBattleServerInstance.ServerIp = battleSettings.GetValue<string>("ServerIP") ??
                                               throw new InvalidOperationException("ServerIP was not found");

            UdpBattleServerInstance.ChannelCapacity = battleSettings.GetValue<int?>("ChannelCapacity") ??
                                                      throw new InvalidOperationException(
                                                          "ChannelCapacity was not found");

            UdpBattleServerInstance.SocketBufferSize = battleSettings.GetValue<int?>("SocketBufferSize") ??
                                                       throw new InvalidOperationException(
                                                           "SocketBufferSize was not found");

            UdpBattleServerInstance.SessionTimeoutSeconds = battleSettings.GetValue<int?>("SessionTimeoutSeconds") ??
                                                            throw new InvalidOperationException(
                                                                "SessionTimeoutSeconds was not found");

            UdpBattleServerInstance.WorkerCount = battleSettings.GetValue<int?>("WorkerCount") ??
                                                  throw new InvalidOperationException("WorkerCount was not found");

            var fsu = configuration.GetConnectionString("FileServerUrl") ??
                      throw new InvalidOperationException("FileServerUrl was not found");

            var fsuChannel = GrpcChannel.ForAddress(fsu, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 256 * 1024 * 1024,
                MaxSendMessageSize = 256 * 1024 * 1024
            });

            GoodbyeMyLoveGoodbye.Init();

            var fsuClient = new FileServerService.FileServerServiceClient(fsuChannel);

            LogicDataTables.LoadFrom(fsuClient);

            services.AddSingleton<ILogicLooperPool>(_ =>
            {
                var looperCount = battleSettings.GetValue<int?>("LoopersInPoolCount") ?? Environment.ProcessorCount * 2;
                return new LogicLooperPool(20, looperCount, RoundRobinLogicLooperPoolBalancer.Instance);
            });

            var mapCacheEntries = LogicMapLoader.GetMapCacheEntriesCount();

            Logger.Info($"Loaded map cache entries: {mapCacheEntries}.");
        });

        builder.UseOrleans((context, siloBuilder) =>
        {
            var configuration = context.Configuration;

            var zk = configuration.GetConnectionString("Clustering_ZooKeeperUrl") ??
                     throw new InvalidOperationException("Clustering_ZooKeeperUrl was not found");

            var ccu = configuration.GetConnectionString("Clustering_ConsulUrl") ??
                      throw new InvalidOperationException("Clustering_ConsulUrl was not found");

            var consulToken = configuration.GetConnectionString("Clustering_ConsulToken") ??
                              throw new InvalidOperationException("Clustering_ConsulToken was not found");

            var clusteringType = (configuration["OrleansC:ClusteringType"] ??
                                  throw new InvalidOperationException("OrleansC:ClusteringType was not found"))
                .Contains("con", StringComparison.CurrentCultureIgnoreCase)
                    ? 1
                    : 2;

            switch (clusteringType)
            {
                case 1:
                    siloBuilder.UseConsulSiloClustering(options =>
                    {
                        options.ConfigureConsulClient(new Uri(ccu), consulToken);
                    });
                    break;
                case 2:
                    siloBuilder.UseZooKeeperClustering(options => { options.ConnectionString = zk; });
                    break;
            }

            siloBuilder.AddMemoryGrainStorage("MemoryStorage");

            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = configuration["OrleansC:ClusterId"];
                options.ServiceId = configuration["OrleansC:ServiceId"];
            });

            siloBuilder.Configure<GrainCollectionOptions>(options =>
            {
                options.CollectionAge = TimeSpan.FromMinutes(
                    configuration.GetValue<int>("OrleansC:GrainCollection:CollectionAgeMinutes"));
                options.DeactivationTimeout = TimeSpan.FromSeconds(
                    configuration.GetValue<int>("OrleansC:GrainCollection:DeactivationTimeoutSeconds"));
            });


            var port = configuration.GetValue<int>("OrleansC:Port");
            siloBuilder.ConfigureEndpoints(11111 + port, 30000 + port);
        });

        var host = builder.Build();

        var config = host.Services.GetRequiredService<IConfiguration>();

        var startPort = config.GetValue<int>("BattleSettings:StartPort");
        var count = config.GetValue<int>("BattleSettings:InstanceCount");

        BattleServerManager.Start(startPort, count);

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");

        await host.RunAsync();
    }
}