using Google.Protobuf.WellKnownTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ZOVserver.Shared.Contracts.Proto;

namespace ZOVserver.Orchestra.GameGlobalEventsBotOrchestrator.Bot;

public class GameGlobalEventsBotHandler(GameGlobalEventsService.GameGlobalEventsServiceClient gEventService)
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null)
            return;

        var message = update.Message;
        var chatId = message.Chat.Id;
        var text = message.Text;

        try
        {
            string response;

            if (text.StartsWith("/add_event"))
            {
                response = await HandleAddEvent(text);
            }
            else if (text.StartsWith("/rem_event"))
            {
                response = await HandleRemoveEvent(text);
            }
            else if (text.StartsWith("/enable_doubletokens_event"))
            {
                await gEventService.SetDoubleTokensEventAsync(new SetDoubleTokensEventAct { Value = true },
                    cancellationToken: cancellationToken);
                response = "Double tokens event enabled";
            }
            else if (text.StartsWith("/disable_doubletokens_event"))
            {
                await gEventService.SetDoubleTokensEventAsync(new SetDoubleTokensEventAct { Value = false },
                    cancellationToken: cancellationToken);
                response = "Double tokens event disabled";
            }
            else if (text.StartsWith("/close_shop"))
            {
                await gEventService.SetShopClosedAsync(new SetShopClosedAct { Value = true },
                    cancellationToken: cancellationToken);
                response = "Shop was closed";
            }
            else if (text.StartsWith("/open_shop"))
            {
                await gEventService.SetShopClosedAsync(new SetShopClosedAct { Value = false },
                    cancellationToken: cancellationToken);
                response = "Shop was opened";
            }
            else if (text.StartsWith("/close_boxes"))
            {
                await gEventService.SetBoxesClosedAsync(new SetBoxesClosedAct { Value = true },
                    cancellationToken: cancellationToken);
                response = "Boxes was closed";
            }
            else if (text.StartsWith("/open_boxes"))
            {
                await gEventService.SetBoxesClosedAsync(new SetBoxesClosedAct { Value = false },
                    cancellationToken: cancellationToken);
                response = "Boxes was opened";
            }
            else if (text.StartsWith("/reset_trophy_season"))
            {
                await gEventService.ResetTrophySeasonAsync(new Empty(), cancellationToken: cancellationToken);
                response = "Trophy season data reset";
            }
            else if (text.StartsWith("/set_lobby_theme"))
            {
                response = await HandleSetLobbyTheme(text);
            }
            else if (text.StartsWith("/start_maintenance"))
            {
                response = await HandleStartMaintenanceAsync(text);
            }
            else if (text.StartsWith("/stop_maintenance"))
            {
                var res = await gEventService.StopMaintenanceAsync(new StopMaintenanceRequest(),
                    cancellationToken: cancellationToken);

                response = res.Result != 0 ? "Unknown error!" : "Maintenance has been stopped";
            }
            else
            {
                response = "Sorry, I only understand these commands:\n" +
                           "/add_event slot locationId [modifiers...] miniBoxTokensReward startTime secondsLifetime\n" +
                           "/rem_event eventId\n" +
                           "/enable_doubletokens_event\n" +
                           "/disable_doubletokens_event\n" +
                           "/close_shop\n" +
                           "/open_shop\n" +
                           "/close_boxes\n" +
                           "/open_boxes\n" +
                           "/reset_trophy_season\n" +
                           "/set_lobby_theme themeGlobalId\n" +
                           "/start_maintenance seconds_left\n" +
                           "/stop_maintenance";
            }

            await botClient.SendMessage(chatId, response, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await botClient.SendMessage(
                chatId,
                $"Error: {ex}",
                cancellationToken: cancellationToken);
        }
    }

    private async Task<string> HandleSetLobbyTheme(string text)
    {
        var args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        if (args.Length < 1)
            return "❌ Error: Not enough arguments.\n" +
                   "Format: /set_lobby_theme themeGlobalId\n" +
                   "Example: /set_lobby_theme 41000000";

        if (!int.TryParse(args[0], out var theme))
            return "❌ Error: 'themeGlobalId' must be a number.";

        var r = await gEventService.SetLobbyThemeAsync(new SetLobbyThemeRequest { LobbyThemeGlobalId = theme });

        return r.Result != 0 ? "Invalid themeGlobalId!" : "The lobby theme is set";
    }

    private async Task<string> HandleStartMaintenanceAsync(string text)
    {
        var args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        if (args.Length < 1)
            return "❌ Error: Not enough arguments.\n" +
                   "Format: /start_maintenance seconds_left\n" +
                   "Example: /start_maintenance 600";

        if (!int.TryParse(args[0], out var seconds))
            return "❌ Error: 'seconds_left' must be a number.";

        if (seconds <= 0)
            return "❌ Error: 'seconds_left' must be greater than zero.";

        var r = await gEventService.StartMaintenanceAsync(new StartMaintenanceRequest { SecondsLeft = seconds });

        return r.Result != 0 ? "Invalid!" : "Maintenance is started";
    }

    private async Task<string> HandleAddEvent(string text)
    {
        var args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        if (args.Length < 5)
            return "❌ Error: Not enough arguments.\n" +
                   "Format: /add_event slot locationId [mod1 mod2 ...] miniBoxTokensReward startTime secondsLifetime\n" +
                   "Example: /add_event 6 15000001 1 2 50 1672531200 3600\n" +
                   "(You can include 0 to 5 modifiers before miniBoxTokensReward)";

        if (!int.TryParse(args[0], out var slot))
            return "❌ Error: 'slot' must be a number.";

        if (!int.TryParse(args[1], out var locationId))
            return "❌ Error: 'locationId' must be a number.";

        var modifiers = new List<int>();
        var currentIndex = 2;

        var endIndex = args.Length - 2;

        if (endIndex < 3)
            return "❌ Error: Invalid number of arguments (modifiers section is too short).";

        while (currentIndex < endIndex - 1)
        {
            if (!int.TryParse(args[currentIndex], out var mod))
                return $"❌ Error: Modifier '{args[currentIndex]}' is not a valid number.";
            modifiers.Add(mod);
            currentIndex++;
        }

        if (modifiers.Count > 5)
            return "❌ Error: Too many modifiers (maximum is 5).";

        if (!int.TryParse(args[endIndex - 1], out var miniBoxTokensReward))
            return "❌ Error: 'miniBoxTokensReward' must be a number.";

        if (!long.TryParse(args[endIndex], out var startTime))
            return "❌ Error: 'startTime' (Unix timestamp) must be a number.";

        if (!int.TryParse(args[endIndex + 1], out var secondsLifetime))
            return "❌ Error: 'secondsLifetime' must be a number.";

        var nevent = new Event
        {
            Slot = slot,
            Location = locationId,
            MiniBoxTokensReward = miniBoxTokensReward,
            StartTime = startTime,
            LifetimeSeconds = secondsLifetime
        };

        nevent.Modifiers.AddRange(modifiers);

        var r = await gEventService.AddNewUpcomingEventAsync(
            new AddNewUpcomingEventRequest { UpcomingEvent = nevent });

        return DescribeAddResult(r.Result, slot, locationId, modifiers, startTime, miniBoxTokensReward,
            secondsLifetime);
    }

    private string DescribeAddResult(int result, int slot, int locationId, List<int> modifiers, long startTime,
        int miniBoxTokensReward, int secondsLifetime)
    {
        var startDateTime = DateTimeOffset.FromUnixTimeSeconds(startTime).DateTime.ToString("yyyy-MM-dd HH:mm");

        return result switch
        {
            -1000 => "❌ Error: Slots 1–5 are reserved and cannot be used.",
            -1001 => $"❌ Error: There's already an upcoming event in slot {slot}.",
            -1002 => $"❌ Error: There's already an active event in slot {slot}.",
            -1003 => "❌ Error: Failed to register event in internal storage (ID conflict?).",
            -1004 => "❌ Error: Too many modifiers (maximum 5 allowed).",
            -1005 => $"❌ Error: 'locationId' ({locationId}) is too low (must be >= 15,000,000) or unknown.",
            > 0 => $"✅ Event successfully added!\n" +
                   $"Event ID: {result}\n" +
                   $"Slot: {slot}\n" +
                   $"Location ID: {locationId}\n" +
                   $"Modifiers: {string.Join(", ", modifiers)}\n" +
                   $"Mini-Box Tokens Reward: {miniBoxTokensReward}\n" +
                   $"Start Time: {startDateTime}\n" +
                   $"Duration: {TimeSpan.FromSeconds(secondsLifetime).TotalMinutes:F1} minutes",
            _ => $"❌ Unknown error {result} occurred."
        };
    }

    private async Task<string> HandleRemoveEvent(string text)
    {
        var args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();

        if (args.Length != 1)
            return "❌ Error: Please specify the event ID.\nFormat: /rem_event eventId";

        if (!int.TryParse(args[0], out var eventId))
            return "❌ Error: Event ID must be a number.";

        var r = await gEventService.RemoveUpcomingEventAsync(
            new RemoveUpcomingEventRequest { EventId = eventId });

        return r.Result switch
        {
            1 => $"✅ Event {eventId} successfully removed.",
            -2005 => $"❌ Error: Event {eventId} not found in internal registry.",
            -2006 => $"❌ Error: Event {eventId} not found in upcoming events list.",
            _ => $"❌ Unknown error ({r.Result}) during removal."
        };
    }
}