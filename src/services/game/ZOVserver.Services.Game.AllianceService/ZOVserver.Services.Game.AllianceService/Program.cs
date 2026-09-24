using System.Reflection;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using ZOVserver.Services.Game.AllianceService.Leaderboard;
using ZOVserver.Services.Game.AllianceService.Settings;
using ZOVserver.Services.Shared.AllianceSearchService;
using ZOVserver.Services.Shared.GarnetLeaderboardsService;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.AllianceService;

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

            var als = fsuClient.GetFile(new FileRequest { Path = "Settings/alliance_settings.yml" }).Data
                .ToStringUtf8();

            AllianceSettings.Load(als);

            LogicDataTables.LoadFrom(fsuClient);

            services.AddSingleton<AllianceOpenSearchWorker>(_ =>
            {
                var openSearchConfig = configuration.GetSection("AllianceOpenSearch");
                var poolConfig = openSearchConfig.GetSection("Pool");
                var connectionConfig = openSearchConfig.GetSection("Connection");
                var indexConfig = openSearchConfig.GetSection("IndexSettings");
                var maxMembers = openSearchConfig.GetValue<int>("MaxMembersInAlliance");

                var nodes = poolConfig.GetSection("Nodes").Get<string[]>() ??
                            throw new InvalidOperationException("AllianceOpenSearch::Pool::Nodes was not found");

                var poolTypeStr = poolConfig["PoolType"] ??
                                  throw new InvalidOperationException(
                                      "AllianceOpenSearch::Pool::PoolType was not found");

                var poolSettings = new PoolSettings
                {
                    Nodes = nodes.Select(n => new Uri(n)).ToArray(),
                    PoolType = Enum.Parse<ConnectionPoolType>(poolTypeStr)
                };

                var connSettings = new ConnSettings
                {
                    Index = connectionConfig["Index"] ??
                            throw new InvalidOperationException("AllianceOpenSearch::Connection::Index was not found"),

                    PingTimeout = TimeSpan.FromSeconds(connectionConfig.GetValue<int>("PingTimeoutSeconds")),
                    RequestTimeout = TimeSpan.FromSeconds(connectionConfig.GetValue<int>("RequestTimeoutSeconds")),

                    MaxRetries = connectionConfig.GetValue<int>("MaxRetries"),
                    RetryTimeout = TimeSpan.FromSeconds(connectionConfig.GetValue<int>("RetryTimeoutSeconds")),

                    UsePrettyJson = connectionConfig.GetValue<bool>("UsePrettyJson"),
                    EnableDebugMode = connectionConfig.GetValue<bool>("EnableDebugMode"),

                    EnableHttpCompression = connectionConfig.GetValue<bool>("EnableHttpCompression"),
                    EnableHttpPipelining = connectionConfig.GetValue<bool>("EnableHttpPipelining")
                };

                var basicAuthSection = connectionConfig.GetSection("BasicAuthentication");
                var basicUser = basicAuthSection["Username"];
                var basicPass = basicAuthSection["Password"];

                if (!string.IsNullOrEmpty(basicUser) && !string.IsNullOrEmpty(basicPass))
                    connSettings.BasicAuthentication = (basicUser, basicPass);

                var apiKeySection = connectionConfig.GetSection("ApiKeyAuthentication");
                var apiKeyId = apiKeySection["Id"];
                var apiKey = apiKeySection["ApiKey"];

                if (!string.IsNullOrEmpty(apiKeyId) && !string.IsNullOrEmpty(apiKey))
                    connSettings.ApiKeyAuthentication = (apiKeyId, apiKey);

                var shards = indexConfig.GetValue<int>("Shards");
                var replicas = indexConfig.GetValue<int>("Replicas");

                return AllianceOpenSearchClient.InitAllianceOpenSearchWorkerAsync(
                               poolSettings, connSettings, shards, replicas, maxMembers)
                           .GetAwaiter().GetResult() ??
                       throw new InvalidOperationException("AllianceOpenSearchClient is null!");
            });

            var garnetSection = configuration.GetSection("GarnetLeaderboard");

            if (!garnetSection.Exists())
                throw new InvalidOperationException("GarnetLeaderboard section was not found in configuration");

            var garnetConfig = garnetSection.Get<GarnetConfig>() ??
                               throw new InvalidOperationException("Failed to bind GarnetConfig");

            LeaderboardContainer.LeaderboardService = new GarnetLeaderboardService(garnetConfig);
        });

        builder.UseOrleans((context, siloBuilder) =>
        {
            var configuration = context.Configuration;

            var mdbu = configuration.GetConnectionString("MongoDBUrl") ??
                       throw new InvalidOperationException("MongoDBUrl was not found");

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

            siloBuilder.UseMongoDBClient(mdbu);

            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = configuration["OrleansC:ClusterId"];
                options.ServiceId = configuration["OrleansC:ServiceId"];
            });

            siloBuilder.AddMongoDBGrainStorage("MongoStorage", options =>
            {
                options.DatabaseName = configuration["OrleansC:MongoStorage:DatabaseName"];
                options.CollectionPrefix = configuration["OrleansC:MongoStorage:CollectionPrefix"];
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

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");
        await builder.RunConsoleAsync();
    }
}