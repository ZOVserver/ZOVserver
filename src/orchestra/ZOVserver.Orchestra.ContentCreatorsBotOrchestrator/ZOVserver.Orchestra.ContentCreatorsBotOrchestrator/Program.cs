using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using ZOVserver.Orchestra.ContentCreatorsBotOrchestrator.Bot;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.TitanRemnants.Helper;

namespace ZOVserver.Orchestra.ContentCreatorsBotOrchestrator;

public static class Program
{
    public static async Task Main(string[] args)
    {
        LogoWriter.ShowLogo();

        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;

            services.AddSingleton<ITelegramBotClient>(_ =>
                new TelegramBotClient(
                    configuration.GetConnectionString("TelegramBotToken") ??
                    throw new InvalidOperationException("TelegramBotToken was not found")));

            services.AddSingleton<ContentCreatorBotHandler>();
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

        var host = builder.Build();
        await host.StartAsync();

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");

        ClientHelper.Client = host.Services.GetService<IClusterClient>() ??
                              throw new InvalidOperationException("Client was not found");

        var botClient = host.Services.GetRequiredService<ITelegramBotClient>();
        var botHandler = host.Services.GetRequiredService<ContentCreatorBotHandler>();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [],
            DropPendingUpdates = true
        };

        var stoppingToken = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await botClient.ReceiveAsync(
                    botHandler.HandleUpdateAsync,
                    (_, exception, _) =>
                    {
                        Console.WriteLine($"Telegram Bot Error: {exception.Message}");
                        return Task.CompletedTask;
                    },
                    receiverOptions,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical Telegram Error: {ex.Message}. Reconnecting in 10s...");
                try
                {
                    await Task.Delay(10000, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

        await host.StopAsync(stoppingToken);
    }
}