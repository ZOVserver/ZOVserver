using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (5) at 1778788288 (c15d70b6-23e5-4f14-a1da-227ecde42b62).
public class LogicResourceData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string IconSwf { get; } = csvElement.GetStringValue("IconSWF");
    public string CollectEffect { get; } = csvElement.GetStringValue("CollectEffect");
    public string IconExportName { get; } = csvElement.GetStringValue("IconExportName");
    public string TypeInCsv { get; } = csvElement.GetStringValue("Type");
    public string Rarity { get; } = csvElement.GetStringValue("Rarity");
    public bool PremiumCurrency { get; } = csvElement.GetBoolValue("PremiumCurrency");
    public int TextRed { get; } = csvElement.GetIntValue("TextRed");
    public int TextGreen { get; } = csvElement.GetIntValue("TextGreen");
    public int TextBlue { get; } = csvElement.GetIntValue("TextBlue");
    public int Cap { get; } = csvElement.GetIntValue("Cap");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicResourceData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]TID[/]");
        table.AddColumn("[bold blue]IconSWF[/]");
        table.AddColumn("[bold blue]CollectEffect[/]");
        table.AddColumn("[bold blue]IconExportName[/]");
        table.AddColumn("[bold blue]Type[/]");
        table.AddColumn("[bold blue]Rarity[/]");
        table.AddColumn("[bold blue]PremiumCurrency[/]");
        table.AddColumn("[bold blue]TextRed[/]");
        table.AddColumn("[bold blue]TextGreen[/]");
        table.AddColumn("[bold blue]TextBlue[/]");
        table.AddColumn("[bold blue]Cap[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Tid}[/]".Replace("\"", ""),
            $"[green]{IconSwf}[/]".Replace("\"", ""), $"[green]{CollectEffect}[/]".Replace("\"", ""),
            $"[green]{IconExportName}[/]".Replace("\"", ""), $"[green]{TypeInCsv}[/]".Replace("\"", ""),
            $"[green]{Rarity}[/]".Replace("\"", ""), $"[green]{PremiumCurrency}[/]", $"[green]{TextRed}[/]",
            $"[green]{TextGreen}[/]", $"[green]{TextBlue}[/]", $"[green]{Cap}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}