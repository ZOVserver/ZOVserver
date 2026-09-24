using Orleans;
using ZOVserver.Shared.Contracts.Structs;

namespace ZOVserver.Shared.Contracts.Events;

/// <summary>
///     Represents a Bridge event in the system.
/// </summary>
[Immutable]
[GenerateSerializer]
[Alias("ZOVserver.Shared.Contracts.Events.BridgeEvent")]
public class BridgeEvent
{
    /// <summary>
    ///     Exclusive event identifier.
    /// </summary>
    [Id(0)] public Guid EventId = Guid.NewGuid();

    /// <summary>
    ///     Gets or sets the type of the event.
    /// </summary>
    /// <remarks>
    ///     Possible values:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>10 - Disconnect event.</description>
    ///         </item>
    ///         <item>
    ///             <description>20 - Send messages event.</description>
    ///         </item>
    ///         <item>
    ///             <description>30 - Disconnect with Messages event.</description>
    ///         </item>
    ///         <item>
    ///             <description>40 - Disconnect with Messages and session re-switching event.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    [Id(1)]
    public byte EventType { get; set; }

    /// <summary>
    ///     Gets or sets the session identifier associated with the event.
    /// </summary>
    [Id(2)]
    public Guid SessionId { get; set; }

    /// <summary>
    ///     Gets or sets an array of Piranha messages related to the event, if any.
    /// </summary>
    [Id(3)]
    public PiranhaMessageStruct[]? PiranhaMessages { get; set; }
}