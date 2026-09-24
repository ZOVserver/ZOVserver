using System.Reflection;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using StackExchange.Redis;
using ZOVserver.Services.Global.LeaderboardEnrichmentService.GarnetLeaderboard;
using ZOVserver.Services.Global.LeaderboardEnrichmentService.Worker;
using ZOVserver.Services.Shared.GarnetLeaderboardsService;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Global.LeaderboardEnrichmentService;

public static class Program
{
    public static async Task Main(string[] args)
    {
        LogoWriter.ShowLogo();

        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;

            var fsu = configuration.GetConnectionString("FileServerUrl") ??
                      throw new InvalidOperationException("FileServerUrl was not found");

            var fsuChannel = GrpcChannel.ForAddress(fsu, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 256 * 1024 * 1024,
                MaxSendMessageSize = 256 * 1024 * 1024
            });

            var fsuClient = new FileServerService.FileServerServiceClient(fsuChannel);

            LogicDataTables.LoadFrom(fsuClient);

            var garnetSection = configuration.GetSection("GarnetLeaderboard");

            if (!garnetSection.Exists())
                throw new InvalidOperationException("GarnetLeaderboard section was not found in configuration");

            var garnetConfig = garnetSection.Get<GarnetConfig>() ??
                               throw new InvalidOperationException("Failed to bind GarnetConfig");

            LeaderboardContainer.LeaderboardService = new GarnetLeaderboardService(garnetConfig);

            var redisUrl = configuration.GetConnectionString("RedisUrl");

            if (string.IsNullOrEmpty(redisUrl))
                throw new InvalidOperationException("RedisUrl not found in ConnectionStrings.");

            var enrichRedisUrl = configuration.GetConnectionString("RedisUrl") ??
                                 throw new InvalidOperationException("RedisUrl not found in ConnectionStrings.");

            var enrichmentConnection = ConnectionMultiplexer.Connect(enrichRedisUrl);

            services.AddSingleton<IConnectionMultiplexer>(enrichmentConnection);

            GlobalLeaderboardEnrichmentWorker.GlobalLeaderboardRefreshIntervalInMins = garnetSection
                    .GetValue<int?>("GlobalLeaderboardRefreshIntervalInMins") ??
                throw new InvalidOperationException(
                    "GarnetLeaderboard:GlobalLeaderboardRefreshIntervalInMins was not found");

            RegionsLeaderboardEnrichmentWorker.RegionsLeaderboardRefreshIntervalInMins = garnetSection
                    .GetValue<int?>("RegionsLeaderboardRefreshIntervalInMins") ??
                throw new InvalidOperationException(
                    "GarnetLeaderboard:RegionsLeaderboardRefreshIntervalInMins was not found");

            services.AddHostedService<GlobalLeaderboardEnrichmentWorker>();
            services.AddHostedService<RegionsLeaderboardEnrichmentWorker>();
        });

        builder.UseOrleansClient(clientBuilder =>
        {
            var configuration = clientBuilder.Configuration;

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
                    clientBuilder.UseConsulClientClustering(options =>
                    {
                        options.ConfigureConsulClient(new Uri(ccu), consulToken);
                    });
                    break;
                case 2:
                    clientBuilder.UseZooKeeperClustering(options => { options.ConnectionString = zk; });
                    break;
            }

            clientBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = configuration["OrleansC:ClusterId"];
                options.ServiceId = configuration["OrleansC:ServiceId"];
            });

            clientBuilder.Configure<GatewayOptions>(opt =>
            {
                opt.GatewayListRefreshPeriod =
                    TimeSpan.FromSeconds(configuration.GetValue<int>("OrleansC:GatewayListRefreshPeriodSecs"));
            });

            clientBuilder.Configure<ClusterMembershipOptions>(opt =>
            {
                opt.TableRefreshTimeout =
                    TimeSpan.FromSeconds(configuration.GetValue<int>("OrleansC:TableRefreshTimeoutSecs"));
                opt.ProbeTimeout = TimeSpan.FromSeconds(configuration.GetValue<int>("OrleansC:ProbeTimeoutSecs"));
                opt.NumMissedProbesLimit = configuration.GetValue<int>("OrleansC:NumMissedProbesLimit");
            });

            clientBuilder.Configure<ClientMessagingOptions>(opt =>
            {
                opt.ResponseTimeout =
                    TimeSpan.FromSeconds(configuration.GetValue<int>("OrleansC:ResponseTimeoutSecs"));
            });
        });

        using var host = builder.Build();

        await host.StartAsync();

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");

        ClientHelper.Client = host.Services.GetRequiredService<IClusterClient>();

        await host.WaitForShutdownAsync();

        Console.WriteLine("Server is shutting down...");
    }
}