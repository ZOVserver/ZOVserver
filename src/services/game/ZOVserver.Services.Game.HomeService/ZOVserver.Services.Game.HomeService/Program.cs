using System.Reflection;
using Consul;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using Orleans.Configuration;
using Prometheus;
using StackExchange.Redis;
using ZOVserver.Services.Game.HomeService.Alliance;
using ZOVserver.Services.Game.HomeService.Leaderboard;
using ZOVserver.Services.Game.HomeService.Manager;
using ZOVserver.Services.Game.HomeService.Settings;
using ZOVserver.Services.Game.HomeService.States.Serialization;
using ZOVserver.Services.Shared.AllianceSearchService;
using ZOVserver.Services.Shared.GarnetLeaderboardsService;
using ZOVserver.Services.Shared.TeamPlayersSearchService;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.Localization;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ClusterOptions = Orleans.Configuration.ClusterOptions;

namespace ZOVserver.Services.Game.HomeService;

public static class Program
{
    public static async Task Main(string[] args)
    {
        LogoWriter.ShowLogo();

        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;

            var metricsSection = configuration.GetSection("MetricsConfig");

            if (!metricsSection.Exists())
                throw new InvalidOperationException("MetricsConfig section was not found in configuration");

            if (metricsSection.GetValue<bool>("Enabled"))
            {
                var orleansPort = configuration.GetSection("OrleansC").GetValue<int?>("Port") ??
                                  throw new InvalidOperationException("OrleansC:Port was not found");

                var basePort = metricsSection.GetValue<int?>("BasePort") ??
                               throw new InvalidOperationException("MetricsConfig:BasePort was not found");

                var myServerIp = metricsSection.GetValue<string>("MyServerIP") ??
                                 throw new InvalidOperationException("MetricsConfig:MyServerIP was not found");

                var metricServer = new MetricServer(myServerIp, basePort + orleansPort);
                metricServer.Start();
            }

            var fsu = configuration.GetConnectionString("FileServerUrl") ??
                      throw new InvalidOperationException("FileServerUrl was not found");

            var fsuChannel = GrpcChannel.ForAddress(fsu, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 256 * 1024 * 1024,
                MaxSendMessageSize = 256 * 1024 * 1024
            });

            var fsuClient = new FileServerService.FileServerServiceClient(fsuChannel);

            var hs = fsuClient.GetFile(new FileRequest { Path = "Settings/home_settings.yml" }).Data.ToStringUtf8();

            LocalizationCache.LoadCache(fsuClient);

            LogicDataTables.LoadFrom(fsuClient);

            HomeSettings.Load(hs);

            var etcdUrl = configuration.GetConnectionString("EtcdUrl") ??
                          throw new InvalidOperationException("EtcdUrl was not found");

            var offersPrefix = configuration.GetSection("EtcdSettings")["OffersPrefix"] ??
                               throw new InvalidOperationException("EtcdSettings::OffersPrefix was not found");

            OffersManager.InitializeAsync(etcdUrl, offersPrefix).Wait();

            var natsSection = configuration.GetSection("GlobalEventsNats");

            if (!natsSection.Exists())
                throw new InvalidOperationException("Section 'GlobalEventsNats' not found");

            var host = natsSection["Host"] ?? throw new InvalidOperationException("NATS Host not found");
            var port = natsSection["Port"] ?? "4222";
            var user = natsSection["Username"];
            var pass = natsSection["Password"];

            var auth = !string.IsNullOrEmpty(user)
                ? new NatsAuthOpts { Username = user, Password = pass }
                : NatsAuthOpts.Default;

            var opts = NatsOpts.Default with
            {
                Url = $"nats://{host}:{port}",
                AuthOpts = auth,
                SerializerRegistry = new NatsJsonSerializerRegistry()
            };

            var nc = new NatsConnection(opts);

            EventsManager.StartConsumerAsync(nc).Wait();

            var openSearchConfig = configuration.GetSection("AllianceOpenSearch");
            var poolConfig = openSearchConfig.GetSection("Pool");
            var connectionConfig = openSearchConfig.GetSection("Connection");
            var indexConfig = openSearchConfig.GetSection("IndexSettings");
            var maxMembers = openSearchConfig.GetValue<int>("MaxMembersInAlliance");

            var nodes = poolConfig.GetSection("Nodes").Get<string[]>() ??
                        throw new InvalidOperationException("AllianceOpenSearch::Pool::Nodes was not found");

            var poolTypeStr = poolConfig["PoolType"] ??
                              throw new InvalidOperationException("AllianceOpenSearch::Pool::PoolType was not found");

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

            OpenSearchWorkerHelper.Worker = AllianceOpenSearchClient.InitAllianceOpenSearchWorkerAsync(
                    poolSettings, connSettings, shards, replicas, maxMembers)
                .GetAwaiter().GetResult() ?? throw new InvalidOperationException("AllianceOpenSearchClient is null!");

            OpenSearchWorkerHelper.AlliancesCache = new MemoryCache(new MemoryCacheOptions());

            var garnetSection = configuration.GetSection("GarnetLeaderboard");

            if (!garnetSection.Exists())
                throw new InvalidOperationException("GarnetLeaderboard section was not found in configuration");

            var garnetConfig = garnetSection.Get<Shared.GarnetLeaderboardsService.GarnetConfig>() ??
                               throw new InvalidOperationException("Failed to bind GarnetConfig");

            LeaderboardContainer.LeaderboardService = new GarnetLeaderboardService(garnetConfig);

            var redisUrl = configuration.GetConnectionString("RedisUrl");

            if (string.IsNullOrEmpty(redisUrl))
                throw new InvalidOperationException("RedisUrl not found in ConnectionStrings.");

            var enrichRedisUrl = configuration.GetConnectionString("RedisUrl") ??
                                 throw new InvalidOperationException("RedisUrl not found in ConnectionStrings.");

            var enrichmentConnection = ConnectionMultiplexer.Connect(enrichRedisUrl);

            services.AddSingleton<IConnectionMultiplexer>(enrichmentConnection);

            LeaderboardWorker.GlobalLeaderboardUpdateIntervalInMins = garnetSection
                                                                          .GetValue<int?>(
                                                                              "GlobalLeaderboardUpdateIntervalInMins") ??
                                                                      throw new InvalidOperationException(
                                                                          "GarnetLeaderboard:GlobalLeaderboardUpdateIntervalInMins was not found");

            LeaderboardWorker.RegionsLeaderboardUpdateIntervalInMins = garnetSection
                                                                           .GetValue<int?>(
                                                                               "RegionsLeaderboardUpdateIntervalInMins") ??
                                                                       throw new InvalidOperationException(
                                                                           "GarnetLeaderboard:RegionsLeaderboardUpdateIntervalInMins was not found");

            services.AddHostedService<LeaderboardWorker>();

            var garnetTSection = configuration.GetSection("GarnetTeamPlayersSearch");

            if (!garnetTSection.Exists())
                throw new InvalidOperationException("GarnetTeamPlayersSearch section was not found in configuration");

            var garnetTConfig = garnetTSection.Get<Shared.TeamPlayersSearchService.GarnetConfig>() ??
                                throw new InvalidOperationException("Failed to bind GarnetConfig");

            services.AddSingleton<ITeamPlayersSearchService>(new GarnetTeamPlayersSearchService(garnetTConfig));
        });

        builder.UseOrleans((context, siloBuilder) =>
        {
            var configuration = context.Configuration;

            var ccu = configuration.GetConnectionString("Clustering_ConsulUrl") ??
                      throw new InvalidOperationException("Clustering_ConsulUrl was not found");

            var consulToken = configuration.GetConnectionString("Clustering_ConsulToken") ??
                              throw new InvalidOperationException("Clustering_ConsulToken was not found");

            var mdbu = configuration.GetConnectionString("MongoDBUrl") ??
                       throw new InvalidOperationException("MongoDBUrl was not found");

            var orleansPort = configuration.GetValue<int?>("OrleansC:Port") ??
                              throw new InvalidOperationException("OrleansC:Port was not found");

            var metricsSection = configuration.GetSection("MetricsConfig");

            var basePort = metricsSection.GetValue<int?>("BasePort") ??
                           throw new InvalidOperationException("MetricsConfig:BasePort was not found");

            var metricPort = basePort + orleansPort;

            var externalIp = metricsSection["ExternalIP"] ??
                             throw new InvalidOperationException("MetricsConfig:ExternalIP was not found");

            var protocol = metricsSection["Protocol"] ?? "http";

            siloBuilder.AddStartupTask(async (_, ct) =>
            {
                var consulClient = new ConsulClient(c =>
                {
                    c.Address = new Uri(ccu);
                    c.Token = consulToken;
                });

                var registration = new AgentServiceRegistration
                {
                    ID = $"silo-{orleansPort}-{Guid.NewGuid().ToString("N")[..8]}",
                    Name = "orleans-silo",

                    Address = externalIp,
                    Port = metricPort,

                    Check = new AgentServiceCheck
                    {
                        HTTP = $"{protocol}://{externalIp}:{metricPort}/metrics",
                        Interval = TimeSpan.FromSeconds(10),
                        DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
                    }
                };

                await consulClient.Agent.ServiceRegister(registration, ct);
            });

            var zk = configuration.GetConnectionString("Clustering_ZooKeeperUrl") ??
                     throw new InvalidOperationException("Clustering_ZooKeeperUrl was not found");

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

                options.GrainStateSerializer = new MessagePackGrainStateSerializer();
            });

            siloBuilder.Configure<GrainCollectionOptions>(options =>
            {
                options.CollectionAge = TimeSpan.FromMinutes(
                    configuration.GetValue<int>("OrleansC:GrainCollection:CollectionAgeMinutes"));
                options.DeactivationTimeout = TimeSpan.FromSeconds(
                    configuration.GetValue<int>("OrleansC:GrainCollection:DeactivationTimeoutSeconds"));
            });


            siloBuilder.ConfigureEndpoints(11111 + orleansPort, 30000 + orleansPort);
        });

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");
        await builder.RunConsoleAsync();
    }
}