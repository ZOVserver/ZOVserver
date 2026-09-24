using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

[LaserSerializable]
public partial class FriendSuggestionsMessage : PiranhaMessage
{
    [Field(3, CountIsI32 = true)] public SuggestionEntry[] FriendSuggestionEntries { get; set; } = [];

    public override int GetMessageType()
    {
        return 20199;
    }

    public override int GetServiceNodeType()
    {
        return 26;
    }
}