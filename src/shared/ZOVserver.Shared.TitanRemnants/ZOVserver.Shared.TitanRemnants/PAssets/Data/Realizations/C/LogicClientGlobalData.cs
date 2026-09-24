using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (9) at 1778788272 (09c8eb0e-55c0-44dc-85fa-7bc7acdfbc38).
public class LogicClientGlobalData(CsvElement csvElement) : LogicData(csvElement)
{
    public int NumberValue { get; } = csvElement.GetIntValue("NumberValue");
    public bool BooleanValue { get; } = csvElement.GetBoolValue("BooleanValue");
    public string TextValue { get; } = csvElement.GetStringValue("TextValue");
    public int NumberArray { get; } = csvElement.GetIntValue("NumberArray");
    public string StringArray { get; } = csvElement.GetStringValue("StringArray");
    public string AltStringArray { get; } = csvElement.GetStringValue("AltStringArray");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicClientGlobalData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]NumberValue[/]");
        table.AddColumn("[bold blue]BooleanValue[/]");
        table.AddColumn("[bold blue]TextValue[/]");
        table.AddColumn("[bold blue]NumberArray[/]");
        table.AddColumn("[bold blue]StringArray[/]");
        table.AddColumn("[bold blue]AltStringArray[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{NumberValue}[/]", $"[green]{BooleanValue}[/]",
            $"[green]{TextValue}[/]".Replace("\"", ""), $"[green]{NumberArray}[/]",
            $"[green]{StringArray}[/]".Replace("\"", ""), $"[green]{AltStringArray}[/]".Replace("\"", ""));

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}