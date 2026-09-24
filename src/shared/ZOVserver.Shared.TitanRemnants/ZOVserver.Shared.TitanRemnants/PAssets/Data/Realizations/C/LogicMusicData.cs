using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (-1) at 1778788272 (9268e6ce-a672-4920-8f51-d73eac088b28).
public class LogicMusicData(CsvElement csvElement) : LogicData(csvElement)
{
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public int Volume { get; } = csvElement.GetIntValue("Volume");
    public bool Loop { get; } = csvElement.GetBoolValue("Loop");
    public int PlayCount { get; } = csvElement.GetIntValue("PlayCount");
    public int FadeOutTimeSec { get; } = csvElement.GetIntValue("FadeOutTimeSec");
    public int DurationSec { get; } = csvElement.GetIntValue("DurationSec");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicMusicData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]FileName[/]");
        table.AddColumn("[bold blue]Volume[/]");
        table.AddColumn("[bold blue]Loop[/]");
        table.AddColumn("[bold blue]PlayCount[/]");
        table.AddColumn("[bold blue]FadeOutTimeSec[/]");
        table.AddColumn("[bold blue]DurationSec[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{FileName}[/]".Replace("\"", ""), $"[green]{Volume}[/]",
            $"[green]{Loop}[/]", $"[green]{PlayCount}[/]", $"[green]{FadeOutTimeSec}[/]", $"[green]{DurationSec}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}