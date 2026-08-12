// -----------------------------------------------------------------------
// <copyright file="FixtureConfrontationSnapshotAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Builds a <see cref="FixtureConfrontationSnapshot"/> from fixture attachments and loaded matches.
/// </summary>
public static class FixtureConfrontationSnapshotAssembler
{
    /// <summary>
    /// Maps fixture attachments and matches into an immutable confrontation snapshot.
    /// </summary>
    public static FixtureConfrontationSnapshot Assemble(Fixture fixture, IReadOnlyList<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(matches);

        var byId = matches.ToDictionary(m => m.Id);
        var legs = new List<FixtureLegSnapshot>(fixture.Attachments.Count);
        foreach (var attachment in fixture.Attachments)
        {
            if (!byId.TryGetValue(attachment.MatchId, out var match))
            {
                throw new ApplicationFailureException(
                    $"Match '{attachment.MatchId}' attached to fixture '{fixture.Id}' was not provided.",
                    ApplicationErrorCodes.FixtureInvalid);
            }

            legs.Add(
                new FixtureLegSnapshot(
                    attachment.LegIndex,
                    match.Id,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    match.Status,
                    match.Result?.Score,
                    match.Result?.ExtraTimePlayed ?? false,
                    match.Result?.PenaltyShootoutScore));
        }

        return new FixtureConfrontationSnapshot(fixture.Id, legs);
    }
}
