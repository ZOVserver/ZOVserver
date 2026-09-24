using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (8) at 1778788272 (0be7a872-4542-4921-ade6-1d2c879f218d).
public class LogicAllianceBadgeData(CsvElement csvElement) : LogicData(csvElement)
{
    public string IconSwf { get; } = csvElement.GetStringValue("IconSWF");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public string Category { get; } = csvElement.GetStringValue("Category");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicAllianceBadgeData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]IconSWF[/]");
        table.AddColumn("[bold blue]IconExportName[/]");
        table.AddColumn("[bold blue]Category[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{IconSwf}[/]".Replace("\"", ""),
            $"[green]{IconExportName}[/]".Replace("\"", ""), $"[green]{Category}[/]".Replace("\"", ""));

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}