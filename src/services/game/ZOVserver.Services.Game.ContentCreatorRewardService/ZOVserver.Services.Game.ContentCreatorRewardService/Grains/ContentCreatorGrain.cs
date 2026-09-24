using System.Security.Cryptography;
using MessagePack;
using NLog;
using Orleans.Providers;
using ZOVserver.Services.Game.ContentCreatorRewardService.Settings;
using ZOVserver.Services.Game.ContentCreatorRewardService.States;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications;
using ZOVserver.Shared.Contracts.Laser.Combined.Notifications.Inheritors;

namespace ZOVserver.Services.Game.ContentCreatorRewardService.Grains;

[StorageProvider(ProviderName = "MongoStorage")]
// ReSharper disable once UnusedType.Global
public class ContentCreatorGrain : Grain<ContentCreatorState>, IContentCreatorRewardServiceGrain
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private bool _save;

    private IDisposable? _tickTimer;

    public async ValueTask Activate()
    {
        State.Activated = true;

        await WriteStateAsync();

        _tickTimer ??= this.RegisterGrainTimer<object?>(
            async _ => await TickAsync(),
            null,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(5),
                Period = TimeSpan.FromSeconds(60),
                Interleave = false
            });
    }

    public async ValueTask Deactivate()
    {
        _save = State.Activated;
        State.Activated = false;

        await WriteStateAsync();

        _tickTimer?.Dispose();
        _tickTimer = null;
    }

    public ValueTask<bool> IsActivated()
    {
        return ValueTask.FromResult(State.Activated);
    }

    public async ValueTask ChangeContentCreatorAccountId(long accountId)
    {
        if (!State.Activated)
            return;

        State.ContentCreatorAccountId = accountId;
        await WriteStateAsync();
    }

    public ValueTask<long> GetContentCreatorAccountId()
    {
        return ValueTask.FromResult(State.ContentCreatorAccountId);
    }

    public ValueTask AddSupporter(long supporterId)
    {
        if (!State.Activated)
            return ValueTask.CompletedTask;

        State.Supporters.Add(supporterId);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveSupporter(long supporterId)
    {
        State.Supporters.Remove(supporterId);
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> GetSupportersCount()
    {
        return ValueTask.FromResult(State.Supporters.Count);
    }

    public ValueTask AddSupportedDiamondsReward(int diamonds)
    {
        if (!State.Activated)
            return ValueTask.CompletedTask;

        var p = ContentCreatorSettings.GetConfig().MagicForLevel * State.ContentCreatorLevel;
        Interlocked.Add(ref State.AlphaDiamondsReward, diamonds * (long)(p * 10_000_000));
        return ValueTask.CompletedTask;
    }

    public ValueTask<List<(DateTime, int)>> GetContentCreatorAccruedRewards()
    {
        return ValueTask.FromResult(State.AccruedRewards);
    }

    public ValueTask<int> GetContentCreatorLevel()
    {
        return ValueTask.FromResult(State.ContentCreatorLevel);
    }

    public async ValueTask SetContentCreatorLevel(int level)
    {
        if (!State.Activated)
            return;

        State.ContentCreatorLevel = level;
        await WriteStateAsync();
    }

    private static int GetRandomInt()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private async Task TickAsync()
    {
        try
        {
            var minDiamondsForReward = ContentCreatorSettings.GetConfig().MinDiamondsForReward;

            var notifications = new List<byte[]>();
            var totalDiamondsToDeduct = 0L;

            var alphaDiamondsReward = Interlocked.Read(ref State.AlphaDiamondsReward);
            var betaDiamonds = alphaDiamondsReward / 10_000_000m;

            while (betaDiamonds >= minDiamondsForReward)
                try
                {
                    var notification = new GemRewardNotification
                    {
                        NotificationIndex = GetRandomInt(),
                        CreationTime = DateTime.UtcNow,
                        Message = $"😎: {await GetSupportersCount()}",
                        Gems = minDiamondsForReward
                    };

                    notifications.Add(MessagePackSerializer.Serialize<BaseNotification>(notification));

                    totalDiamondsToDeduct += minDiamondsForReward;
                    betaDiamonds -= minDiamondsForReward;

                    Logger.Info($"Added {minDiamondsForReward} gems to {State.ContentCreatorAccountId}!");
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error processing diamond reward notification");
                    break;
                }

            if (notifications.Count > 0)
                try
                {
                    var home = GrainHelper.GetHomeGrain(GrainFactory, State.ContentCreatorAccountId);
                    await home.AddNotifications(notifications.ToArray());

                    State.AccruedRewards.Add((DateTime.UtcNow, minDiamondsForReward * notifications.Count));
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to send notifications: {0} | {1}", State.ContentCreatorAccountId,
                        e.ToString());

                    totalDiamondsToDeduct = 0;
                    notifications.Clear();
                }

            if (totalDiamondsToDeduct > 0)
                Interlocked.Add(ref State.AlphaDiamondsReward, -totalDiamondsToDeduct * 10_000_000);

            await WriteStateAsync();
        }
        catch (Exception e)
        {
            Logger.Error(e, "Unexpected error in TickAsync");
            throw;
        }
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (State.Activated)
            _tickTimer ??= this.RegisterGrainTimer<object?>(
                async _ => await TickAsync(),
                null,
                new GrainTimerCreationOptions
                {
                    DueTime = TimeSpan.FromSeconds(5),
                    Period = TimeSpan.FromSeconds(60),
                    Interleave = false
                });

        return base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _tickTimer?.Dispose();
        _tickTimer = null;

        if (State.Activated || _save)
            await WriteStateAsync();
        _save = false;

        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}