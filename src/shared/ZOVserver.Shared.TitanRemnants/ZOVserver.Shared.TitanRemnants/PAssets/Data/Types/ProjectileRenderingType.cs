using System.Collections.Frozen;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Types;

public static class ProjectileRenderingType
{
    public static readonly FrozenDictionary<string, int> Values = new Dictionary<string, int>
    {
        { "Use360Frames", 1 },
        { "UseZFrames", 2 },
        { "DoNotRotateClip", 3 },
        { "UseVerticalMirroring", 4 }
    }.ToFrozenDictionary();
}