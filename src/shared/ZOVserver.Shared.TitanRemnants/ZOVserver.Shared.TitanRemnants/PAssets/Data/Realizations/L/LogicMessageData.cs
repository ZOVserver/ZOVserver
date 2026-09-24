using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (40) at 1778788288 (f34925a1-8332-4f23-bd86-ca027309a42e).
public class LogicMessageData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string BubbleOverrideTid { get; } = csvElement.GetStringValue("BubbleOverrideTID");
    public bool Disabled { get; } = csvElement.GetBoolValue("Disabled");
    public int MessageType { get; } = csvElement.GetIntValue("MessageType");
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string ExportName { get; } = csvElement.GetStringValue("ExportName");
    public int QuickEmojiType { get; } = csvElement.GetIntValue("QuickEmojiType");
    public int SortPriority { get; } = csvElement.GetIntValue("SortPriority");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicMessageData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]TID[/]");
        table.AddColumn("[bold blue]BubbleOverrideTID[/]");
        table.AddColumn("[bold blue]Disabled[/]");
        table.AddColumn("[bold blue]MessageType[/]");
        table.AddColumn("[bold blue]FileName[/]");
        table.AddColumn("[bold blue]ExportName[/]");
        table.AddColumn("[bold blue]QuickEmojiType[/]");
        table.AddColumn("[bold blue]SortPriority[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Tid}[/]".Replace("\"", ""),
            $"[green]{BubbleOverrideTid}[/]".Replace("\"", ""), $"[green]{Disabled}[/]", $"[green]{MessageType}[/]",
            $"[green]{FileName}[/]".Replace("\"", ""), $"[green]{ExportName}[/]".Replace("\"", ""),
            $"[green]{QuickEmojiType}[/]", $"[green]{SortPriority}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}