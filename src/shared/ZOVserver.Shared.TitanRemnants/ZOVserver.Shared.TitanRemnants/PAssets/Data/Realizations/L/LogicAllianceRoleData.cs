using Spectre.Console;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (25) at 1778788272 (334cb282-9760-4a9d-82b8-fda2ba91e2e9).
public class LogicAllianceRoleData(CsvElement csvElement) : LogicData(csvElement)
{
    public int Level { get; } = csvElement.GetIntValue("Level");
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public bool CanInvite { get; } = csvElement.GetBoolValue("CanInvite");
    public bool CanSendMail { get; } = csvElement.GetBoolValue("CanSendMail");
    public bool CanChangeAllianceSettings { get; } = csvElement.GetBoolValue("CanChangeAllianceSettings");
    public bool CanAcceptJoinRequest { get; } = csvElement.GetBoolValue("CanAcceptJoinRequest");
    public bool CanKick { get; } = csvElement.GetBoolValue("CanKick");
    public bool CanBePromotedToLeader { get; } = csvElement.GetBoolValue("CanBePromotedToLeader");
    public int PromoteSkill { get; } = csvElement.GetIntValue("PromoteSkill");

    public override string ToString()
    {
        var table = new Table
        {
            Title = new TableTitle("LogicAllianceRoleData")
        };

        table.AddColumn("[bold blue]Name[/]");
        table.AddColumn("[bold blue]Level[/]");
        table.AddColumn("[bold blue]TID[/]");
        table.AddColumn("[bold blue]CanInvite[/]");
        table.AddColumn("[bold blue]CanSendMail[/]");
        table.AddColumn("[bold blue]CanChangeAllianceSettings[/]");
        table.AddColumn("[bold blue]CanAcceptJoinRequest[/]");
        table.AddColumn("[bold blue]CanKick[/]");
        table.AddColumn("[bold blue]CanBePromotedToLeader[/]");
        table.AddColumn("[bold blue]PromoteSkill[/]");
        table.AddRow($"[green]{Name}[/]", $"[green]{Level}[/]", $"[green]{Tid}[/]".Replace("\"", ""),
            $"[green]{CanInvite}[/]", $"[green]{CanSendMail}[/]", $"[green]{CanChangeAllianceSettings}[/]",
            $"[green]{CanAcceptJoinRequest}[/]", $"[green]{CanKick}[/]", $"[green]{CanBePromotedToLeader}[/]",
            $"[green]{PromoteSkill}[/]");

        using var writer = new StringWriter();
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }).Write(table);
        return writer.ToString();
    }
}