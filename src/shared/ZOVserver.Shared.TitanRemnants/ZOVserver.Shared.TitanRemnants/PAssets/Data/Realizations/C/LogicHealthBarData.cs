using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (11) at 1778788272 (36986cf8-d10d-496a-a816-dffc6f957979).
public class LogicHealthBarData(CsvElement csvElement) : LogicData(csvElement)
{
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string PlayerExportNameTop { get; } = csvElement.GetStringValue("PlayerExportNameTop");
    public string PlayerExportNameBot { get; } = csvElement.GetStringValue("PlayerExportNameBot");
    public string EnemyExportNameTop { get; } = csvElement.GetStringValue("EnemyExportNameTop");
    public string EnemyExportNameBot { get; } = csvElement.GetStringValue("EnemyExportNameBot");
    public string YourTeamExportNameTop { get; } = csvElement.GetStringValue("YourTeamExportNameTop");
    public string YourTeamExportNameBot { get; } = csvElement.GetStringValue("YourTeamExportNameBot");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicHealthBarData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]FileName[/]");
        table.AddColumn("[bold blue]PlayerExportNameTop[/]");
        table.AddColumn("[bold blue]PlayerExportNameBot[/]");
        table.AddColumn("[bold blue]EnemyExportNameTop[/]");
        table.AddColumn("[bold blue]EnemyExportNameBot[/]");
        table.AddColumn("[bold blue]YourTeamExportNameTop[/]");
        table.AddColumn("[bold blue]YourTeamExportNameBot[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{FileName}[/]".Replace("\"", ""),
            $"[green]{PlayerExportNameTop}[/]".Replace("\"", ""), $"[green]{PlayerExportNameBot}[/]".Replace("\"", ""),
            $"[green]{EnemyExportNameTop}[/]".Replace("\"", ""), $"[green]{EnemyExportNameBot}[/]".Replace("\"", ""),
            $"[green]{YourTeamExportNameTop}[/]".Replace("\"", ""),
            $"[green]{YourTeamExportNameBot}[/]".Replace("\"", ""));

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}