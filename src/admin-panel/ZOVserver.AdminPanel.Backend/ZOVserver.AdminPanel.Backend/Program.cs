using System.Reflection;
using dotnet_etcd;
using Grpc.Core;
using Grpc.Net.Client;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using ZOVserver.AdminPanel.Backend.Services;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.Helper;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ClusterOptions = Orleans.Configuration.ClusterOptions;

namespace ZOVserver.AdminPanel.Backend;

public static class Program
{
    public static async Task Main(string[] args)
    {
        LogoWriter.ShowLogo();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGrpc();

        builder.Host.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;

            var etcdUrl = configuration.GetConnectionString("EtcdUrl") ??
                          throw new InvalidOperationException("EtcdUrl was not found");

            var prefix = configuration.GetSection("EtcdSettings")["OffersPrefix"] ??
                         throw new InvalidOperationException("EtcdSettings::OffersPrefix was not found");

            var isHttps = etcdUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase);

            var sharedEtcdClient = new EtcdClient(etcdUrl, configureChannelOptions: options =>
            {
                if (isHttps)
                {
                    options.Credentials = ChannelCredentials.SecureSsl;

                    var httpHandler = new HttpClientHandler();

#if DEBUG
                    httpHandler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif

                    options.HttpHandler = httpHandler;
                }
                else
                {
                    options.Credentials = ChannelCredentials.Insecure;
                }
            });

            services.AddSingleton(sharedEtcdClient);
            services.AddSingleton(new ShopOffersClient(sharedEtcdClient, prefix));

            var fsu = configuration.GetConnectionString("FileServerUrl") ??
                      throw new InvalidOperationException("FileServerUrl was not found");

            var fsuChannel = GrpcChannel.ForAddress(fsu, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 256 * 1024 * 1024,
                MaxSendMessageSize = 256 * 1024 * 1024
            });

            var fsuClient = new FileServerService.FileServerServiceClient(fsuChannel);

            LogicDataTables.LoadFrom(fsuClient);
        });

        builder.Services.AddHttpClient<MetricsAggregatorService>(client =>
        {
            var mimirUrl = builder.Configuration["MimirConfig:Url"] ??
                           throw new InvalidOperationException("MimirConfig::Url was not found");

            client.BaseAddress = new Uri(mimirUrl.TrimEnd('/') + "/");
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

        var app = builder.Build();

        app.MapGrpcService<GiftsService>();
        app.MapGrpcService<MessagesService>();
        app.MapGrpcService<ShopService>();
        app.MapGrpcService<StatisticsService>();
        app.MapGrpcService<UserManagementService>();

        ClientHelper.Client = app.Services.GetService<IClusterClient>() ??
                              throw new InvalidOperationException("Client was not found");

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");

        await app.RunAsync();
    }
}