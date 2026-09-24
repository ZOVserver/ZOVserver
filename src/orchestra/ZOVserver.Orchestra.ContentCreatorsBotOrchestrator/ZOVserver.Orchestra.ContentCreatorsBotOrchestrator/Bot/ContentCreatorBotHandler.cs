using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.TitanRemnants.Utility;

namespace ZOVserver.Orchestra.ContentCreatorsBotOrchestrator.Bot;

public class ContentCreatorBotHandler(ITelegramBotClient client, IClusterClient orleansClient)
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } messageText } message)
            return;

        var chatId = message.Chat.Id;

        try
        {
            var commandParts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (commandParts.Length == 0)
                return;

            var command = commandParts[0].ToLower();
            var args = commandParts.Skip(1).ToArray();

            switch (command)
            {
                case "/help":
                    await SendHelpMessage(chatId, cancellationToken);
                    break;

                case "/activate_new_content_creator":
                    await HandleActivateNewCreator(chatId, args, cancellationToken);
                    break;

                case "/deactivate_content_creator":
                    await HandleDeactivateCreator(chatId, args, cancellationToken);
                    break;

                case "/get_supporters_count":
                    await HandleGetSupportersCount(chatId, args, cancellationToken);
                    break;

                case "/change_content_creator_acc_id":
                    await HandleChangeAccountId(chatId, args, cancellationToken);
                    break;

                case "/get_content_creator_acc_id":
                    await HandleGetAccountId(chatId, args, cancellationToken);
                    break;

                case "/get_content_creator_level":
                    await HandleGetCreatorLevel(chatId, args, cancellationToken);
                    break;

                case "/set_content_creator_level":
                    await HandleSetCreatorLevel(chatId, args, cancellationToken);
                    break;

                case "/get_content_creator_accrued_rewards":
                    await HandleGetAccruedRewards(chatId, args, cancellationToken);
                    break;

                default:
                    await client.SendMessage(
                        chatId,
                        "Unknown command. Type /help for available commands.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            await client.SendMessage(
                chatId,
                $"Error: {ex}",
                cancellationToken: cancellationToken);
        }
    }

    private async Task SendHelpMessage(long chatId, CancellationToken cancellationToken)
    {
        var helpText = new StringBuilder();

        helpText.AppendLine("Available commands:");
        helpText.AppendLine("/help - Show this help message");
        helpText.AppendLine(
            "/activate_new_content_creator [creator_code] [level] [account_tag] - Activate new content creator");
        helpText.AppendLine("/deactivate_content_creator [creator_code] - Deactivate content creator");
        helpText.AppendLine("/get_supporters_count [creator_code] - Get supporters count");
        helpText.AppendLine(
            "/change_content_creator_acc_id [creator_code] [account_tag] - Change content creator account ID");
        helpText.AppendLine("/get_content_creator_acc_id [creator_code] - Get content creator account ID");
        helpText.AppendLine("/get_content_creator_level [creator_code] - Get content creator level");
        helpText.AppendLine("/set_content_creator_level [creator_code] [level] - Set content creator level");
        helpText.AppendLine(
            "/get_content_creator_accrued_rewards [creator_code] - Get content creator accrued rewards");

        await client.SendMessage(
            chatId,
            helpText.ToString(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleActivateNewCreator(long chatId, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 3)
        {
            await client.SendMessage(
                chatId,
                "Invalid arguments. Usage: /activate_new_content_creator [creator_code] [level] [account_tag]",
                cancellationToken: cancellationToken);
            return;
        }

        var creatorCode = args[0];

        if (!int.TryParse(args[1], out var level))
        {
            await client.SendMessage(
                chatId,
                "Invalid level. Must be a number.",
                cancellationToken: cancellationToken);
            return;
        }

        var accountId = new LogicLongToCodeConverterUtil().ToId(args[2]);

        if (accountId < 1)
        {
            await client.SendMessage(
                chatId,
                "Invalid account ID. Must be a correct tag.",
                cancellationToken: cancellationToken);
            return;
        }

        var player = ClientHelper.GetPlayerSession(accountId);

        if (await player.GetSessionsCount() <= 1)
        {
            await client.SendMessage(
                chatId,
                "Error: Sessions count must be more than 1!",
                cancellationToken: cancellationToken);
            return;
        }

        var grain = orleansClient.GetGrain<IContentCreatorRewardServiceGrain>(creatorCode);

        if (await grain.IsActivated())
        {
            await client.SendMessage(
                chatId,
                $"Error: Content creator with code {creatorCode} is already activated.",
                cancellationToken: cancellationToken);
            return;
        }

        await grain.Activate();
        await grain.SetContentCreatorLevel(level);
        await grain.ChangeContentCreatorAccountId(accountId);

        await client.SendMessage(
            chatId,
            $"Successfully activated content creator with code {creatorCode}, level {level}, and account ID {accountId}.",
            cancellationToken: cancellationToken);
    }

    private async Task HandleDeactivateCreator(long chatId, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 1)
        {
            await client.SendMessage(
                chatId,
                "Invalid arguments. Usage: /deactivate_content_creator [creator_code]",
                cancellationToken: cancellationToken);
            return;
        }

        var creatorCode = args[0];

        var grain = orleansClient.GetGrain<IContentCreatorRewardServiceGrain>(creatorCode);

        if (!await grain.IsActivated())
        {
            await client.SendMessage(
                chatId,
                $"Error: Content creator with code {creatorCode} is not activated.",
                cancellationToken: cancellationToken);
            return;
        }

        await grain.Deactivate();

        await client.SendMessage(
            chatId,
            $"Successfully deactivated content creator with code {creatorCode}.",
            cancellationToken: cancellationToken);
    }

    private async Task HandleGetSupportersCount(long chatId, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 1)
        {
            await client.SendMessage(
                chatId,
                "Invalid arguments. Usage: /get_supporters_count [creator_code]",
                cancellationToken: cancellationToken);
            return;
        }

        var creatorCode = args[0];

        var grain = orleansClient.GetGrain<IContentCreatorRewardServiceGrain>(creatorCode);

        if (!await grain.IsActivated())
        {
            await client.SendMessage(
                chatId,
                $"Error: Content creator with code {creatorCode} is not activated.",
                cancellationToken: cancellationToken);
            return;
        }

        var count = await grain.GetSupportersCount();

        await client.SendMessage(
            chatId,
            $"Content creator with code {creatorCode} has {count} supporters.",
            cancellationToken: cancellationToken);
    }

    private async Task HandleChangeAccountId(long chatId, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 2)
        {
            await client.SendMessage(
                chatId,
                "Invalid arguments. Usage: /change_content_creator_acc_id [creator_code] [account_tag]",
                cancellationToken: cancellationToken);
            return;
        }

        var creatorCode = args[0];

        var accountId = new LogicLongToCodeConverterUtil().ToId(args[2]);

        if (accountId < 1)
        {
            await client.SendMessage(
                chatId,
                "Invalid account ID. Must be a correct tag.",
                cancellationToken: cancellationToken);
            return;
        }

        var player = ClientHelper.GetPlayerSession(accountId);

        if (await player.GetSessionsCount() <= 1)
        {
            await client.SendMessage(
                chatId,
                "Error: Sessions count must be more than 1!",
                cancellationToken: cancellationToken);
            return;
        }

        var grain = orleansClient.GetGrain<IContentCreatorRewardServiceGrain>(creatorCode);

        if (!await grain.IsActivated())
        {
            await client.SendMessage(
                chatId,
                $"Error: Content creator with code {creatorCode} is not activated.",
                cancellationToken: cancellationToken);
            return;
        }

        await grain.ChangeContentCreatorAccountId(accountId);

        await client.SendMessage(
            chatId,
            $"Successfully changed account ID to {accountId} for content creator with code {creatorCode}.",
            cancellationToken: cancellationToken);
    }

    private async Task HandleGetAccountId(long chatId, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 1)
        {
            await client.SendMessage(
                chatId,
                "Invalid arguments. Usage: /get_content_creator_acc_id [creator_code]",
                cancellationToken: cancellationToken);
            return;
        }

        var creatorCode = args[0];

        var grain = orleansClient.GetGrain<IContentCreatorRewardServiceGrain>(creatorCode);

        if (!await grain.IsActivated())
        {
            await client.SendMessage(
                chatId,
                $"Error: Content creator with code {creatorCode} is not activated.",
                cancellationToken: cancellationToken);
            return;
        }

        var accountId = await grain.GetContentCreatorAccountId();

        await client.SendMessage(
            chatId,
            $"Content creator with code {creatorCode} has account ID {accountId}.",
            cancellationToken: cancellationToken);
    }

    private async Task HandleGetCreatorLevel(long chatId, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 1)
        {
            await client.SendMessage(
                chatId,
                "Invalid arguments. Usage: /get_content_creator_level [creator_code]",
                cancellationToken: cancellationToken);
            return;
        }

        var creatorCode = args[0];
        var grain = orleansClient.GetGrain<IContentCreatorRewardServiceGrain>(creatorCode);

        if (!await grain.IsActivated())
        {
            await client.SendMessage(
                chatId,
                $"Error: Content creator with code {creatorCode} is not activated.",
                cancellationToken: cancellationToken);
            return;
        }

        var level = await grain.GetContentCreatorLevel();

        await client.SendMessage(
            chatId,
            $"Content creator with code {creatorCode} has level {level}.",
            cancellationToken: cancellationToken);
    }

    private async Task HandleSetCreatorLevel(long chatId, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 2)
        {
            await client.SendMessage(
                chatId,
                "Invalid arguments. Usage: /set_content_creator_level [creator_code] [level]",
                cancellationToken: cancellationToken);
            return;
        }

        var creatorCode = args[0];
        if (!int.TryParse(args[1], out var level))
        {
            await client.SendMessage(
                chatId,
                "Invalid level. Must be a number.",
                cancellationToken: cancellationToken);
            return;
        }

        var grain = orleansClient.GetGrain<IContentCreatorRewardServiceGrain>(creatorCode);

        if (!await grain.IsActivated())
        {
            await client.SendMessage(
                chatId,
                $"Error: Content creator with code {creatorCode} is not activated.",
                cancellationToken: cancellationToken);
            return;
        }

        await grain.SetContentCreatorLevel(level);

        await client.SendMessage(
            chatId,
            $"Successfully set level to {level} for content creator with code {creatorCode}.",
            cancellationToken: cancellationToken);
    }

    private async Task HandleGetAccruedRewards(long chatId, string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 1)
        {
            await client.SendMessage(
                chatId,
                "Invalid arguments. Usage: /get_content_creator_accrued_rewards [creator_code]",
                cancellationToken: cancellationToken);
            return;
        }

        var creatorCode = args[0];

        var grain = orleansClient.GetGrain<IContentCreatorRewardServiceGrain>(creatorCode);

        if (!await grain.IsActivated())
        {
            await client.SendMessage(
                chatId,
                $"Error: Content creator with code {creatorCode} is not activated.",
                cancellationToken: cancellationToken);
            return;
        }

        var rewards = await grain.GetContentCreatorAccruedRewards();

        var response = new StringBuilder();
        response.AppendLine($"Accrued rewards for content creator with code {creatorCode}:");

        if (rewards.Count != 0)
            foreach (var (date, amount) in rewards)
                response.AppendLine($"{date}: {amount} diamonds");
        else
            response.AppendLine("No rewards yet.");

        await client.SendMessage(
            chatId,
            response.ToString(),
            cancellationToken: cancellationToken);
    }
}