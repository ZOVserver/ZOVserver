using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (35) at 1778788288 (42400c26-5bb6-4710-b1ab-75be601eba33).
public class LogicPinData(CsvElement csvElement) : LogicData(csvElement)
{
    public int PinType { get; } = csvElement.GetIntValue("PinType");
    public int Rarity { get; } = csvElement.GetIntValue("Rarity");
    public int Index { get; } = csvElement.GetIntValue("Index");
    public int Bonus { get; } = csvElement.GetIntValue("Bonus");
    public int CraftCost { get; } = csvElement.GetIntValue("CraftCost");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicPinData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]PinType[/]");
        table.AddColumn("[bold blue]Rarity[/]");
        table.AddColumn("[bold blue]Index[/]");
        table.AddColumn("[bold blue]Bonus[/]");
        table.AddColumn("[bold blue]CraftCost[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{PinType}[/]", $"[green]{Rarity}[/]", $"[green]{Index}[/]",
            $"[green]{Bonus}[/]", $"[green]{CraftCost}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}