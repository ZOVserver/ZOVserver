using System.Reflection;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using Orleans.Configuration;
using ZOVserver.Services.Game.TeamService.Manager;
using ZOVserver.Services.Game.TeamService.Settings;
using ZOVserver.Services.Shared.TeamPlayersSearchService;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.TeamService;

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

            var tss = fsuClient.GetFile(new FileRequest { Path = "Settings/team_settings.yml" }).Data
                .ToStringUtf8();

            TeamSettings.Load(tss);

            LogicDataTables.LoadFrom(fsuClient);

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

            var garnetSection = configuration.GetSection("GarnetTeamPlayersSearch");

            if (!garnetSection.Exists())
                throw new InvalidOperationException("GarnetTeamPlayersSearch section was not found in configuration");

            var garnetConfig = garnetSection.Get<GarnetConfig>() ??
                               throw new InvalidOperationException("Failed to bind GarnetConfig");

            services.AddSingleton<ITeamPlayersSearchService>(new GarnetTeamPlayersSearchService(garnetConfig));
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

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");
        await builder.RunConsoleAsync();
    }
}