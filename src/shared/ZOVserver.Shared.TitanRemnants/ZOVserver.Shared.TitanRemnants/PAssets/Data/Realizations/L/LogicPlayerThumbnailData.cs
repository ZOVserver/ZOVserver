using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (28) at 1778788288 (07fd262f-2683-4d22-a4a7-110825445719).
public class LogicPlayerThumbnailData(CsvElement csvElement) : LogicData(csvElement)
{
    public int RequiredExpLevel { get; } = csvElement.GetIntValue("RequiredExpLevel");
    public int RequiredTotalTrophies { get; } = csvElement.GetIntValue("RequiredTotalTrophies");
    public int RequiredSeasonPoints { get; } = csvElement.GetIntValue("RequiredSeasonPoints");
    public string RequiredHero { get; } = csvElement.GetStringValue("RequiredHero");
    public string IconSwf { get; } = csvElement.GetStringValue("IconSWF");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public int SortOrder { get; } = csvElement.GetIntValue("SortOrder");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicPlayerThumbnailData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]RequiredExpLevel[/]");
        table.AddColumn("[bold blue]RequiredTotalTrophies[/]");
        table.AddColumn("[bold blue]RequiredSeasonPoints[/]");
        table.AddColumn("[bold blue]RequiredHero[/]");
        table.AddColumn("[bold blue]IconSWF[/]");
        table.AddColumn("[bold blue]IconExportName[/]");
        table.AddColumn("[bold blue]SortOrder[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{RequiredExpLevel}[/]", $"[green]{RequiredTotalTrophies}[/]",
            $"[green]{RequiredSeasonPoints}[/]", $"[green]{RequiredHero}[/]".Replace("\"", ""),
            $"[green]{IconSwf}[/]".Replace("\"", ""), $"[green]{IconExportName}[/]".Replace("\"", ""),
            $"[green]{SortOrder}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}