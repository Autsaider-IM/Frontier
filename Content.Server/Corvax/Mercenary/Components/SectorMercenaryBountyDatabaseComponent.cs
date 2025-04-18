using Content.Shared.Corvax.Mercenary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Corvax.Mercenary.Components;

/// <summary>
/// Stores all active cargo bounties for a particular station.
/// </summary>
[RegisterComponent]
public sealed partial class SectorMercenaryBountyDatabaseComponent : Component
{
    /// <summary>
    /// Maximum amount of bounties a station can have.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxBounties = 6;

    /// <summary>
    /// A list of all the bounties currently active for a station.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<MercenaryBountyData> Bounties = new();

    /// <summary>
    /// Used to determine unique order IDs
    /// </summary>
    [DataField]
    public int TotalBounties;

    /// <summary>
    /// The time at which players will be able to skip the next bounty.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSkipTime = TimeSpan.Zero;

    /// <summary>
    /// The time between skipping bounties.
    /// </summary>
    [DataField]
    public TimeSpan SkipDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The time between cancelling bounties.
    /// </summary>
    [DataField]
    public TimeSpan CancelDelay = TimeSpan.FromMinutes(30);
}
