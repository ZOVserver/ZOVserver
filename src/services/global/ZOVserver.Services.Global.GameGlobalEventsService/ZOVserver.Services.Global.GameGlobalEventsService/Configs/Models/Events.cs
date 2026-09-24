namespace ZOVserver.Services.Global.GameGlobalEventsService.Configs.Models;

public class Events
{
    public bool GenerateGemGrab { get; set; }
    public bool GenerateShowdown { get; set; }
    public bool GenerateDailyEvents { get; set; }
    public bool GenerateSpecialEvents { get; set; }

    public List<string> DailyEventsGameModes { get; set; } = [];
    public List<string> SpecialEventsGameModes { get; set; } = [];
}