using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (13) at 1778788272 (9ed316e9-df4a-463b-b26f-4d2345e69bf8).
public class LogicCreditData(CsvElement csvElement) : LogicData(csvElement)
{
    public int _0 { get; } = csvElement.GetIntValue("0");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicCreditData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]0[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{_0}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}