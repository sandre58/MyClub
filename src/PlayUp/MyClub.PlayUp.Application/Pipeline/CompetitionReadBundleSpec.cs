// -----------------------------------------------------------------------
// <copyright file="CompetitionReadBundleSpec.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Read-intent shape for competition-scoped bundle loads (not a Domain aggregate variant).
/// </summary>
/// <param name="StageCapabilities">Stage graph composition.</param>
/// <param name="MatchProfile">Match load depth.</param>
public sealed record CompetitionReadBundleSpec(
    StageReadCapabilities StageCapabilities,
    MatchLoadProfile MatchProfile)
{
    /// <summary>Needs Attention + workspace summary.</summary>
    public static CompetitionReadBundleSpec Attention { get; } = new(
        new StageReadCapabilities(
            StageLoadProfile.Structure,
            IncludeDraws: true,
            IncludeSlots: true,
            IncludePenalties: true),
        MatchLoadProfile.AttentionSlice);

    /// <summary>Overview, consultation, organisation (unchanged heavy reads).</summary>
    public static CompetitionReadBundleSpec Overview { get; } = new(
        StageReadCapabilities.FromProfile(StageLoadProfile.Full),
        MatchLoadProfile.Full);
}
