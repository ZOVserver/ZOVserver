using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (41) at 1778788289 (206cd935-4609-4c09-a88d-f25ae9313959).
public class LogicThemeData(CsvElement csvElement) : LogicData(csvElement)
{
    public string FileName { get; } = csvElement.GetStringValue("FileName");
    public string ExportName { get; } = csvElement.GetStringValue("ExportName");
    public string ParticleFileName { get; } = csvElement.GetStringValue("ParticleFileName");
    public string ParticleExportName { get; } = csvElement.GetStringValue("ParticleExportName");
    public string ParticleStyle { get; } = csvElement.GetStringValue("ParticleStyle");
    public int ParticleVariations { get; } = csvElement.GetIntValue("ParticleVariations");
    public string ThemeMusic { get; } = csvElement.GetStringValue("ThemeMusic");
    public bool UseInLevelSelection { get; } = csvElement.GetBoolValue("UseInLevelSelection");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicThemeData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]FileName[/]");
        table.AddColumn("[bold blue]ExportName[/]");
        table.AddColumn("[bold blue]ParticleFileName[/]");
        table.AddColumn("[bold blue]ParticleExportName[/]");
        table.AddColumn("[bold blue]ParticleStyle[/]");
        table.AddColumn("[bold blue]ParticleVariations[/]");
        table.AddColumn("[bold blue]ThemeMusic[/]");
        table.AddColumn("[bold blue]UseInLevelSelection[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{FileName}[/]".Replace("\"", ""),
            $"[green]{ExportName}[/]".Replace("\"", ""), $"[green]{ParticleFileName}[/]".Replace("\"", ""),
            $"[green]{ParticleExportName}[/]".Replace("\"", ""), $"[green]{ParticleStyle}[/]".Replace("\"", ""),
            $"[green]{ParticleVariations}[/]", $"[green]{ThemeMusic}[/]".Replace("\"", ""),
            $"[green]{UseInLevelSelection}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}