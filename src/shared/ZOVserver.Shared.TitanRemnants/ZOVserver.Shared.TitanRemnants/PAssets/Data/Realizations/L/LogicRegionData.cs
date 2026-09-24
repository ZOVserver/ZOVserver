using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (14) at 1778788288 (635ae6eb-ca51-4d5d-8d3e-7ea783683d6a).
public class LogicRegionData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string DisplayName { get; } = csvElement.GetStringValue("DisplayName");
    public bool IsCountry { get; } = csvElement.GetBoolValue("IsCountry");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicRegionData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]TID[/]");
        table.AddColumn("[bold blue]DisplayName[/]");
        table.AddColumn("[bold blue]IsCountry[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Tid}[/]".Replace("\"", ""),
            $"[green]{DisplayName}[/]".Replace("\"", ""), $"[green]{IsCountry}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}