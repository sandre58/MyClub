// -----------------------------------------------------------------------
// <copyright file="StageReadCapabilities.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Composes stage graph depth for a read use case (base profile + optional collections).
/// </summary>
/// <param name="Profile">Base graph: Summary, Structure, or Full.</param>
/// <param name="IncludeDraws">Include draw aggregates when profile is not Full.</param>
/// <param name="IncludeSlots">Include slot assignments when profile is not Full.</param>
/// <param name="IncludePenalties">Include standing penalties when profile is not Full.</param>
public sealed record StageReadCapabilities(
    StageLoadProfile Profile,
    bool IncludeDraws = false,
    bool IncludeSlots = false,
    bool IncludePenalties = false)
{
    /// <summary>Maps profile-only loads to capabilities.</summary>
    public static StageReadCapabilities FromProfile(StageLoadProfile profile) =>
        profile switch
        {
            StageLoadProfile.Full => new StageReadCapabilities(StageLoadProfile.Full),
            StageLoadProfile.Structure => new StageReadCapabilities(StageLoadProfile.Structure),
            StageLoadProfile.Summary => new StageReadCapabilities(StageLoadProfile.Summary),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };
}
