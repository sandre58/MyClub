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
    /// <summary>Gets needs Attention + workspace summary.</summary>
    public static CompetitionReadBundleSpec Attention { get; } = new(
        new StageReadCapabilities(
            StageLoadProfile.Structure,
            IncludeDraws: true,
            IncludeSlots: true,
            IncludePenalties: true),
        MatchLoadProfile.AttentionSlice);

    /// <summary>Gets overview (unchanged heavy read).</summary>
    public static CompetitionReadBundleSpec Overview { get; } = new(
        StageReadCapabilities.FromProfile(StageLoadProfile.Full),
        MatchLoadProfile.Full);

    /// <summary>Gets consultation — full stage graph, projected match rows.</summary>
    public static CompetitionReadBundleSpec Consultation { get; } = new(
        StageReadCapabilities.FromProfile(StageLoadProfile.Full),
        MatchLoadProfile.SummaryRow);

    /// <summary>Gets structure hub — topology + slots, no match aggregates.</summary>
    public static CompetitionReadBundleSpec Structure { get; } = new(
        new StageReadCapabilities(StageLoadProfile.Structure, IncludeSlots: true),
        MatchLoadProfile.None);
}
