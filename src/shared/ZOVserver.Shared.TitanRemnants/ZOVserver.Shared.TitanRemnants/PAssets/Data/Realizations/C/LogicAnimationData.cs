using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (24) at 1778788271 (9444675e-0998-4aa9-af9f-eb76fa40a9ba).
public class LogicAnimationData(CsvElement csvElement) : LogicData(csvElement)
{
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public int StartFrame { get; } = csvElement.GetIntValue("StartFrame");
    public int EndFrame { get; } = csvElement.GetIntValue("EndFrame");
    public int Speed { get; } = csvElement.GetIntValue("Speed");
    public int TransitionInMs { get; } = csvElement.GetIntValue("TransitionInMs");
    public int TransitionOutMs { get; } = csvElement.GetIntValue("TransitionOutMs");
    public int AutoFadeMs { get; } = csvElement.GetIntValue("AutoFadeMs");
    public bool Looping { get; } = csvElement.GetBoolValue("Looping");
    public string Priority { get; } = csvElement.GetStringValue("Priority");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicAnimationData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]FileName[/]");
        table.AddColumn("[bold blue]StartFrame[/]");
        table.AddColumn("[bold blue]EndFrame[/]");
        table.AddColumn("[bold blue]Speed[/]");
        table.AddColumn("[bold blue]TransitionInMs[/]");
        table.AddColumn("[bold blue]TransitionOutMs[/]");
        table.AddColumn("[bold blue]AutoFadeMs[/]");
        table.AddColumn("[bold blue]Looping[/]");
        table.AddColumn("[bold blue]Priority[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{FileName}[/]".Replace("\"", ""), $"[green]{StartFrame}[/]",
            $"[green]{EndFrame}[/]", $"[green]{Speed}[/]", $"[green]{TransitionInMs}[/]",
            $"[green]{TransitionOutMs}[/]", $"[green]{AutoFadeMs}[/]", $"[green]{Looping}[/]",
            $"[green]{Priority}[/]".Replace("\"", ""));

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}