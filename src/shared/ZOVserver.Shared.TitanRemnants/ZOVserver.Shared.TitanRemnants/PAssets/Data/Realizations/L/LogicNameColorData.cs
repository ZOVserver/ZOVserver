using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (43) at 1778788288 (0b3b87d5-8b71-4bb4-9981-0a5ff2fa1c42).
public class LogicNameColorData(CsvElement csvElement) : LogicData(csvElement)
{
    public string ColorCode { get; } = csvElement.GetStringValue("ColorCode");
    public string Gradient { get; } = csvElement.GetStringValue("Gradient");
    public int RequiredExpLevel { get; } = csvElement.GetIntValue("RequiredExpLevel");
    public int RequiredTotalTrophies { get; } = csvElement.GetIntValue("RequiredTotalTrophies");
    public int RequiredSeasonPoints { get; } = csvElement.GetIntValue("RequiredSeasonPoints");
    public string RequiredHero { get; } = csvElement.GetStringValue("RequiredHero");
    public int SortOrder { get; } = csvElement.GetIntValue("SortOrder");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicNameColorData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]ColorCode[/]");
        table.AddColumn("[bold blue]Gradient[/]");
        table.AddColumn("[bold blue]RequiredExpLevel[/]");
        table.AddColumn("[bold blue]RequiredTotalTrophies[/]");
        table.AddColumn("[bold blue]RequiredSeasonPoints[/]");
        table.AddColumn("[bold blue]RequiredHero[/]");
        table.AddColumn("[bold blue]SortOrder[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{ColorCode}[/]".Replace("\"", ""),
            $"[green]{Gradient}[/]".Replace("\"", ""), $"[green]{RequiredExpLevel}[/]",
            $"[green]{RequiredTotalTrophies}[/]", $"[green]{RequiredSeasonPoints}[/]",
            $"[green]{RequiredHero}[/]".Replace("\"", ""), $"[green]{SortOrder}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}