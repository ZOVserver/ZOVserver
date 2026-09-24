using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (36) at 1778788272 (aa614fda-055a-449c-83bb-d2ef9a104128).
public class LogicHintData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public int MinXpLevel { get; } = csvElement.GetIntValue("MinXPLevel");
    public int MaxXpLevel { get; } = csvElement.GetIntValue("MaxXPLevel");
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string ExportName { get; } = csvElement.GetStringValue("ExportName");
    public string Character { get; } = csvElement.GetStringValue("Character");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicHintData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]TID[/]");
        table.AddColumn("[bold blue]MinXPLevel[/]");
        table.AddColumn("[bold blue]MaxXPLevel[/]");
        table.AddColumn("[bold blue]FileName[/]");
        table.AddColumn("[bold blue]ExportName[/]");
        table.AddColumn("[bold blue]Character[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Tid}[/]".Replace("\"", ""), $"[green]{MinXpLevel}[/]",
            $"[green]{MaxXpLevel}[/]", $"[green]{FileName}[/]".Replace("\"", ""),
            $"[green]{ExportName}[/]".Replace("\"", ""), $"[green]{Character}[/]".Replace("\"", ""));

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}