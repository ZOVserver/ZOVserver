using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (46) at 1778788272 (e3511aa3-c5b5-47da-8bb1-7b0336baf73c).
public class LogicColorGradientData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Colors { get; } = csvElement.GetStringValue("Colors");
    public int Speed { get; } = csvElement.GetIntValue("Speed");
    public int Scale { get; } = csvElement.GetIntValue("Scale");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicColorGradientData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]Colors[/]");
        table.AddColumn("[bold blue]Speed[/]");
        table.AddColumn("[bold blue]Scale[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Colors}[/]".Replace("\"", ""), $"[green]{Speed}[/]",
            $"[green]{Scale}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}