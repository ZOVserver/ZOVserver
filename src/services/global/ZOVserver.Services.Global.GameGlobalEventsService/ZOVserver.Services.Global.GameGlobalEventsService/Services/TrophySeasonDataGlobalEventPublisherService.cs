using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using ZOVserver.Services.Global.GameGlobalEventsService.FileProvider;
using ZOVserver.Services.Global.GameGlobalEventsService.Grpc;
using ZOVserver.Shared.Contracts.Events;

namespace ZOVserver.Services.Global.GameGlobalEventsService.Services;

public class TrophySeasonDataGlobalEventPublisherService(
    INatsConnection nats,
    ILogger<TrophySeasonDataGlobalEventPublisherService> logger) : BackgroundService
{
    private readonly ThreadSafeFileProvider<TrophySeasonGlobalEvent> _season =
        new(Path.GetFullPath(@"Configs\trophy_season.yml"));

    private bool _reset;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        GameGlobalEventsGrpcService.TrophySeasonService = this;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var season = _season.GetData();

                if (season.SeasonEndTime < DateTime.UtcNow || _reset)
                {
                    _season.SaveData(new TrophySeasonGlobalEvent
                    {
                        SeasonCounter = season.SeasonCounter + 1,
                        SeasonEndTime = DateTime.UtcNow.AddMinutes(season.SeasonDurationMinutes),
                        SeasonDurationMinutes = season.SeasonDurationMinutes
                    });

                    season = _season.GetData();
                    _reset = false;

                    logger.LogInformation("Trophy season {SeasonCounter} started!", season.SeasonCounter);
                }

                var ge = new TrophySeasonGlobalEvent
                {
                    SeasonCounter = season.SeasonCounter,
                    SeasonEndTime = season.SeasonEndTime.AddMilliseconds(666 * 1.5)
                };

                await nats.PublishAsync("GlobalEvents.TrophySeason", ge, cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            await Task.Delay(250, stoppingToken);
        }
    }

    public void ResetTrophySeason()
    {
        _reset = true;
    }
}