using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (48) at 1778788288 (3d996aa9-5ff7-43a1-9bc9-7d3813a262aa).
public class LogicGameModeVariationData(CsvElement csvElement) : LogicData(csvElement)
{
    public int Variation { get; } = csvElement.GetIntValue("Variation");
    public bool Disabled { get; } = csvElement.GetBoolValue("Disabled");
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string ChatSuggestionItemName { get; } = csvElement.GetStringValue("ChatSuggestionItemName");
    public string GameModeRoomIconName { get; } = csvElement.GetStringValue("GameModeRoomIconName");
    public string GameModeIconName { get; } = csvElement.GetStringValue("GameModeIconName");
    public string ScoreSfx { get; } = csvElement.GetStringValue("ScoreSfx");
    public string OpponentScoreSfx { get; } = csvElement.GetStringValue("OpponentScoreSfx");
    public string ScoreText { get; } = csvElement.GetStringValue("ScoreText");
    public string ScoreTextEnd { get; } = csvElement.GetStringValue("ScoreTextEnd");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicGameModeVariationData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]Variation[/]");
        table.AddColumn("[bold blue]Disabled[/]");
        table.AddColumn("[bold blue]TID[/]");
        table.AddColumn("[bold blue]ChatSuggestionItemName[/]");
        table.AddColumn("[bold blue]GameModeRoomIconName[/]");
        table.AddColumn("[bold blue]GameModeIconName[/]");
        table.AddColumn("[bold blue]ScoreSfx[/]");
        table.AddColumn("[bold blue]OpponentScoreSfx[/]");
        table.AddColumn("[bold blue]ScoreText[/]");
        table.AddColumn("[bold blue]ScoreTextEnd[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Variation}[/]", $"[green]{Disabled}[/]",
            $"[green]{Tid}[/]".Replace("\"", ""), $"[green]{ChatSuggestionItemName}[/]".Replace("\"", ""),
            $"[green]{GameModeRoomIconName}[/]".Replace("\"", ""), $"[green]{GameModeIconName}[/]".Replace("\"", ""),
            $"[green]{ScoreSfx}[/]".Replace("\"", ""), $"[green]{OpponentScoreSfx}[/]".Replace("\"", ""),
            $"[green]{ScoreText}[/]".Replace("\"", ""), $"[green]{ScoreTextEnd}[/]".Replace("\"", ""));

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}