using System.Reflection;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using ZOVserver.Services.Game.FriendshipService.Settings;
using ZOVserver.Services.Game.FriendshipService.States.Serialization;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Game.FriendshipService;

public static class Program
{
    public static async Task Main(string[] args)
    {
        LogoWriter.ShowLogo();

        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((context, _) =>
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

            var fs = fsuClient.GetFile(new FileRequest { Path = "Settings/friendship_settings.yml" }).Data
                .ToStringUtf8();

            FriendshipSettings.Load(fs);
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

                options.GrainStateSerializer = new MessagePackGrainStateSerializer();
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