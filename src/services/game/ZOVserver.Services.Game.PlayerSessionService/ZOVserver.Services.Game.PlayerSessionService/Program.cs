using System.Reflection;
using Consul;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Prometheus;
using ZOVserver.Services.Game.PlayerSessionService.States.Serialization;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.Localization;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.PlayerSessionService;

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
                      throw new InvalidOperationException("ConnectionStrings:FileServerUrl was not found");

            var fsuChannel = GrpcChannel.ForAddress(fsu, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 256 * 1024 * 1024,
                MaxSendMessageSize = 256 * 1024 * 1024
            });

            var fsuClient = new FileServerService.FileServerServiceClient(fsuChannel);

            LocalizationCache.LoadCache(fsuClient);
            LogicDataTables.LoadFrom(fsuClient);
        });

        builder.UseOrleans((context, siloBuilder) =>
        {
            var configuration = context.Configuration;

            var ccu = configuration.GetConnectionString("Clustering_ConsulUrl") ??
                      throw new InvalidOperationException("ConnectionStrings:Clustering_ConsulUrl was not found");

            var consulToken = configuration.GetConnectionString("Clustering_ConsulToken") ??
                              throw new InvalidOperationException("Clustering_ConsulToken was not found");

            var mdbu = configuration.GetConnectionString("MongoDBUrl") ??
                       throw new InvalidOperationException("ConnectionStrings:MongoDBUrl was not found");

            var orleansC = configuration.GetSection("OrleansC");

            if (!orleansC.Exists())
                throw new InvalidOperationException("OrleansC section was not found");

            var orleansPort = orleansC.GetValue<int?>("Port") ??
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
                options.ClusterId = orleansC["ClusterId"] ??
                                    throw new InvalidOperationException("OrleansC:ClusterId was not found");
                options.ServiceId = orleansC["ServiceId"] ??
                                    throw new InvalidOperationException("OrleansC:ServiceId was not found");
            });

            siloBuilder.AddMongoDBGrainStorage("MongoStorage", options =>
            {
                options.DatabaseName = orleansC["MongoStorage:DatabaseName"] ??
                                       throw new InvalidOperationException(
                                           "OrleansC:MongoStorage:DatabaseName was not found");
                options.CollectionPrefix = orleansC["MongoStorage:CollectionPrefix"] ??
                                           throw new InvalidOperationException(
                                               "OrleansC:MongoStorage:CollectionPrefix was not found");
                options.GrainStateSerializer = new MessagePackGrainStateSerializer();
            });

            siloBuilder.Configure<GrainCollectionOptions>(options =>
            {
                options.CollectionAge = TimeSpan.FromMinutes(
                    orleansC.GetSection("GrainCollection").GetValue<int?>("CollectionAgeMinutes") ?? 2);
                options.DeactivationTimeout = TimeSpan.FromSeconds(
                    orleansC.GetSection("GrainCollection").GetValue<int?>("DeactivationTimeoutSeconds") ?? 15);
            });


            siloBuilder.ConfigureEndpoints(11111 + orleansPort, 30000 + orleansPort);
        });

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");
        await builder.RunConsoleAsync();
    }
}