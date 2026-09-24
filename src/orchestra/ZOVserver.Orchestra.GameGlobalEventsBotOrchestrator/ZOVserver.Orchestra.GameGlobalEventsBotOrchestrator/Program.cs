using System.Reflection;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using ZOVserver.Orchestra.GameGlobalEventsBotOrchestrator.Bot;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.Helper;

namespace ZOVserver.Orchestra.GameGlobalEventsBotOrchestrator;

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

            services.AddSingleton<GameGlobalEventsBotHandler>();

            var ggesu = configuration.GetConnectionString("GameGlobalEventsServiceUrl") ??
                        throw new InvalidOperationException("GameGlobalEventsServiceUrl was not found");
            var ggesuChannel = GrpcChannel.ForAddress(ggesu);

            var ggesc = new GameGlobalEventsService.GameGlobalEventsServiceClient(ggesuChannel);

            services.AddSingleton(ggesc);
        });

        var host = builder.Build();
        await host.StartAsync();

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");

        var botClient = host.Services.GetRequiredService<ITelegramBotClient>();
        var botHandler = host.Services.GetRequiredService<GameGlobalEventsBotHandler>();

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