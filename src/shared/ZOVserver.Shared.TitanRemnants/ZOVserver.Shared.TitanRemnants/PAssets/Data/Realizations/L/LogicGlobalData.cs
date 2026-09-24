using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (3) at 1778788288 (5aa590e1-7e74-4e9d-a79d-fe73be85e546).
public class LogicGlobalData(CsvElement csvElement) : LogicData(csvElement)
{
    public int NumberValue { get; } = csvElement.GetIntValue("NumberValue");
    public bool BooleanValue { get; } = csvElement.GetBoolValue("BooleanValue");
    public string TextValue { get; } = csvElement.GetStringValue("TextValue");
    public string StringArray { get; } = csvElement.GetStringValue("StringArray");
    public int NumberArray { get; } = csvElement.GetIntValue("NumberArray");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicGlobalData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]NumberValue[/]");
        table.AddColumn("[bold blue]BooleanValue[/]");
        table.AddColumn("[bold blue]TextValue[/]");
        table.AddColumn("[bold blue]StringArray[/]");
        table.AddColumn("[bold blue]NumberArray[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{NumberValue}[/]", $"[green]{BooleanValue}[/]",
            $"[green]{TextValue}[/]".Replace("\"", ""), $"[green]{StringArray}[/]".Replace("\"", ""),
            $"[green]{NumberArray}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}