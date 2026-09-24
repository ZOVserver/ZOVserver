using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (30) at 1778788272 (9753e7b2-a69a-4bcb-a5e1-74e1421e4b5a).
public class LogicFaceData(CsvElement csvElement) : LogicData(csvElement)
{
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string ExportName { get; } = csvElement.GetStringValue("ExportName");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicFaceData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]FileName[/]");
        table.AddColumn("[bold blue]ExportName[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{FileName}[/]".Replace("\"", ""),
            $"[green]{ExportName}[/]".Replace("\"", ""));

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}