using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (-1) at 1778788288 (d6e8a6ea-6f3d-4b87-90fd-0d8af2ebe659).
public class LogicSkinsrarityData(CsvElement csvElement) : LogicData(csvElement)
{
    public int Price { get; } = csvElement.GetIntValue("Price");
    public int Rarity { get; } = csvElement.GetIntValue("Rarity");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicSkinsrarityData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]Price[/]");
        table.AddColumn("[bold blue]Rarity[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Price}[/]", $"[green]{Rarity}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}