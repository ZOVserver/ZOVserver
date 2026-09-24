using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.C;

// Generated (4) at 1778788272 (2d663e62-5115-43d1-8e4c-b8072c6b2679).
public class LogicSoundData(CsvElement csvElement) : LogicData(csvElement)
{
    public string FileNames { get; } = csvElement.GetStringValue("FileNames");
    public int MinVolume { get; } = csvElement.GetIntValue("MinVolume");
    public int MaxVolume { get; } = csvElement.GetIntValue("MaxVolume");
    public int MinPitch { get; } = csvElement.GetIntValue("MinPitch");
    public int MaxPitch { get; } = csvElement.GetIntValue("MaxPitch");
    public int Priority { get; } = csvElement.GetIntValue("Priority");
    public int MaximumByType { get; } = csvElement.GetIntValue("MaximumByType");
    public int MaxRepeatMs { get; } = csvElement.GetIntValue("MaxRepeatMs");
    public bool Loop { get; } = csvElement.GetBoolValue("Loop");
    public bool PlayVariationsInSequence { get; } = csvElement.GetBoolValue("PlayVariationsInSequence");

    public bool PlayVariationsInSequenceManualReset { get; } =
        csvElement.GetBoolValue("PlayVariationsInSequenceManualReset");

    public int StartDelayMinMs { get; } = csvElement.GetIntValue("StartDelayMinMs");
    public int StartDelayMaxMs { get; } = csvElement.GetIntValue("StartDelayMaxMs");
    public bool PlayOnlyWhenInView { get; } = csvElement.GetBoolValue("PlayOnlyWhenInView");
    public int MaxVolumeScaleLimit { get; } = csvElement.GetIntValue("MaxVolumeScaleLimit");
    public int NoSoundScaleLimit { get; } = csvElement.GetIntValue("NoSoundScaleLimit");
    public int PadEmpyToEndMs { get; } = csvElement.GetIntValue("PadEmpyToEndMs");

    public override string ToString()
    {
        return $"""
                LogicSoundData =>
                   Name = {Name},
                   FileNames = {FileNames},
                   MinVolume = {MinVolume},
                   MaxVolume = {MaxVolume},
                   MinPitch = {MinPitch},
                   MaxPitch = {MaxPitch},
                   Priority = {Priority},
                   MaximumByType = {MaximumByType},
                   MaxRepeatMs = {MaxRepeatMs},
                   Loop = {Loop},
                   PlayVariationsInSequence = {PlayVariationsInSequence},
                   PlayVariationsInSequenceManualReset = {PlayVariationsInSequenceManualReset},
                   StartDelayMinMs = {StartDelayMinMs},
                   StartDelayMaxMs = {StartDelayMaxMs},
                   PlayOnlyWhenInView = {PlayOnlyWhenInView},
                   MaxVolumeScaleLimit = {MaxVolumeScaleLimit},
                   NoSoundScaleLimit = {NoSoundScaleLimit},
                   PadEmpyToEndMs = {PadEmpyToEndMs}
                """;
    }
}