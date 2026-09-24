using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (42) at 1778788272 (e285f956-8f09-41e1-ae07-e435b8b6e8de).
public class LogicLinkData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Language { get; } = csvElement.GetStringValue("Language");
    public string URL { get; } = csvElement.GetStringValue("URL");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicLinkData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]Language[/]");
        table.AddColumn("[bold blue]URL[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Language}[/]".Replace("\"", ""),
            $"[green]{URL}[/]".Replace("\"", ""));

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}