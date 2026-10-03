// -----------------------------------------------------------------------
// <copyright file="MaterializedParticipantSnapshots.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Builds Domain participant snapshots from loaded Match aggregates for F1 coherence gates.
/// </summary>
internal static class MaterializedParticipantSnapshots
{
    /// <summary>
    /// Maps matches to <see cref="MatchParticipantSnapshot"/> (Home/Away only).
    /// </summary>
    public static IReadOnlyList<MatchParticipantSnapshot> FromMatches(IEnumerable<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);
        return [.. matches.Select(match => new MatchParticipantSnapshot(
            match.Id,
            match.HomeEntryId,
            match.AwayEntryId))];
    }
}
