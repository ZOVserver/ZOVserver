using System.Reflection;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using Orleans.Configuration;
using ZOVserver.Bridge.TcpProtocolBridge.Manager;
using ZOVserver.Bridge.TcpProtocolBridge.Networking;
using ZOVserver.RasputinProtect;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.Localization;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using static ZOVserver.Bridge.TcpHellomateGirl.TcpHellomateGirl;

namespace ZOVserver.Bridge.TcpProtocolBridge;

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

            LocalizationCache.LoadCache(fsuClient);

            LogicDataTables.LoadFrom(fsuClient);

            var serversConfig = configuration.GetSection("Servers:TcpServers");
            var tcpServers = serversConfig.Get<Dictionary<string, int>>() ?? new Dictionary<string, int>();

            foreach (var server in tcpServers)
                DotNettyTcpServer.Start(Convert.ToUInt16(server.Key), server.Value).Wait();

            GoodbyeMyLoveGoodbye.Init();
            var keys = GoodbyeMyLoveGoodbye.GenerateServerKeys();
            GoodbyeMyLoveGoodbye.ChvI(keys.Item1, keys.Item2);

            Init();
            CreateServerHelloWithKey(Convert.FromHexString(keys.Item2));

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

            Console.WriteLine();
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

        DotNettyTcpServer.DoesAcceptConnections = true;

        await host.WaitForShutdownAsync();

        Console.WriteLine("Server is shutting down...");
    }
}