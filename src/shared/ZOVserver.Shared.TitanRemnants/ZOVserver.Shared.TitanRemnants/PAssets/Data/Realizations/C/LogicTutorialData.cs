using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (-1) at 1778788272 (1ca62a1e-67c6-4b2e-9ff9-7fd880731a60).
public class LogicTutorialData(CsvElement csvElement) : LogicData(csvElement)
{
    public string StepName { get; } = csvElement.GetStringValue("StepName");
    public int StartDelayMs { get; } = csvElement.GetIntValue("StartDelayMS");
    public int EndDelayMs { get; } = csvElement.GetIntValue("EndDelayMS");
    public int ForceSpeechBubbleCloseMs { get; } = csvElement.GetIntValue("ForceSpeechBubbleCloseMS");
    public string StartCondition { get; } = csvElement.GetStringValue("StartCondition");
    public int StartLocationX { get; } = csvElement.GetIntValue("StartLocationX");
    public int StartLocationY { get; } = csvElement.GetIntValue("StartLocationY");
    public int StartLocationRadius { get; } = csvElement.GetIntValue("StartLocationRadius");
    public int AnimationX { get; } = csvElement.GetIntValue("AnimationX");
    public int AnimationY { get; } = csvElement.GetIntValue("AnimationY");
    public int AnimationX2 { get; } = csvElement.GetIntValue("AnimationX2");
    public int AnimationY2 { get; } = csvElement.GetIntValue("AnimationY2");
    public string CompleteCondition { get; } = csvElement.GetStringValue("CompleteCondition");
    public bool ShouldUseAutoShoot { get; } = csvElement.GetBoolValue("ShouldUseAutoShoot");
    public int CompleteLocationX { get; } = csvElement.GetIntValue("CompleteLocationX");
    public int CompleteLocationY { get; } = csvElement.GetIntValue("CompleteLocationY");
    public int CompleteLocationRadius { get; } = csvElement.GetIntValue("CompleteLocationRadius");
    public int UseUltiX { get; } = csvElement.GetIntValue("UseUltiX");
    public int UseUltiY { get; } = csvElement.GetIntValue("UseUltiY");
    public string AnimationClipSwf { get; } = csvElement.GetStringValue("AnimationClipSWF");
    public string AnimationMovieClip { get; } = csvElement.GetStringValue("AnimationMovieClip");
    public string AnimationClipSwf2 { get; } = csvElement.GetStringValue("AnimationClipSWF2");
    public string AnimationMovieClip2 { get; } = csvElement.GetStringValue("AnimationMovieClip2");
    public string SpeechBubbleCharacterSwf { get; } = csvElement.GetStringValue("SpeechBubbleCharacterSWF");
    public string SpeechBubbleCharacterMovieClip { get; } = csvElement.GetStringValue("SpeechBubbleCharacterMovieClip");
    public string SpeechBubbleTids { get; } = csvElement.GetStringValue("SpeechBubbleTIDs");
    public string StartSound { get; } = csvElement.GetStringValue("StartSound");
    public string SpawnCharacter { get; } = csvElement.GetStringValue("SpawnCharacter");
    public int SpawnLocationX { get; } = csvElement.GetIntValue("SpawnLocationX");
    public int SpawnLocationY { get; } = csvElement.GetIntValue("SpawnLocationY");
    public int CustomData { get; } = csvElement.GetIntValue("CustomData");
    public bool BlockingSpeechBubble { get; } = csvElement.GetBoolValue("BlockingSpeechBubble");
    public bool ShowUlti { get; } = csvElement.GetBoolValue("ShowUlti");
    public bool ShowShootStick { get; } = csvElement.GetBoolValue("ShowShootStick");
    public bool LeftSpeechBubble { get; } = csvElement.GetBoolValue("LeftSpeechBubble");

    public override string ToString()
    {
        return $"""
                LogicTutorialData =>
                   Name = {Name},
                   StepName = {StepName},
                   StartDelayMS = {StartDelayMs},
                   EndDelayMS = {EndDelayMs},
                   ForceSpeechBubbleCloseMS = {ForceSpeechBubbleCloseMs},
                   StartCondition = {StartCondition},
                   StartLocationX = {StartLocationX},
                   StartLocationY = {StartLocationY},
                   StartLocationRadius = {StartLocationRadius},
                   AnimationX = {AnimationX},
                   AnimationY = {AnimationY},
                   AnimationX2 = {AnimationX2},
                   AnimationY2 = {AnimationY2},
                   CompleteCondition = {CompleteCondition},
                   ShouldUseAutoShoot = {ShouldUseAutoShoot},
                   CompleteLocationX = {CompleteLocationX},
                   CompleteLocationY = {CompleteLocationY},
                   CompleteLocationRadius = {CompleteLocationRadius},
                   UseUltiX = {UseUltiX},
                   UseUltiY = {UseUltiY},
                   AnimationClipSWF = {AnimationClipSwf},
                   AnimationMovieClip = {AnimationMovieClip},
                   AnimationClipSWF2 = {AnimationClipSwf2},
                   AnimationMovieClip2 = {AnimationMovieClip2},
                   SpeechBubbleCharacterSWF = {SpeechBubbleCharacterSwf},
                   SpeechBubbleCharacterMovieClip = {SpeechBubbleCharacterMovieClip},
                   SpeechBubbleTIDs = {SpeechBubbleTids},
                   StartSound = {StartSound},
                   SpawnCharacter = {SpawnCharacter},
                   SpawnLocationX = {SpawnLocationX},
                   SpawnLocationY = {SpawnLocationY},
                   CustomData = {CustomData},
                   BlockingSpeechBubble = {BlockingSpeechBubble},
                   ShowUlti = {ShowUlti},
                   ShowShootStick = {ShowShootStick},
                   LeftSpeechBubble = {LeftSpeechBubble}
                """;
    }
}