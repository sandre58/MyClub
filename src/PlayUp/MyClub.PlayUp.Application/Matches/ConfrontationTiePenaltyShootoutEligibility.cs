// -----------------------------------------------------------------------
// <copyright file="ConfrontationTiePenaltyShootoutEligibility.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Decides whether a penalty shootout on an unequal final-leg play score may be recorded (aggregate still level).
/// </summary>
internal static class ConfrontationTiePenaltyShootoutEligibility
{
    /// <summary>
    /// Returns whether the proposed played result on the final leg may use the confrontation tie shootout flag.
    /// </summary>
    public static async Task<bool> IsEligibleAsync(
        Stage stage,
        Match match,
        MatchResultEntry entry,
        IMatchRepository matches,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(matches);

        if (entry.Type != ResultType.Played)
        {
            return false;
        }

        if (entry.PenaltyShootoutHomeGoals is null || entry.PenaltyShootoutAwayGoals is null)
        {
            return false;
        }

        var playScore = new Score(entry.HomeGoals, entry.AwayGoals);
        if (playScore.HomeGoals == playScore.AwayGoals)
        {
            return false;
        }

        var fixtureId = stage.FindFixtureIdContainingMatch(match.Id);
        if (fixtureId is null)
        {
            return false;
        }

        var fixture = stage.FindFixture(fixtureId.Value);
        if (fixture is null)
        {
            return false;
        }

        var round = stage.Rounds.FirstOrDefault(r => r.Fixtures.Any(f => f.Id.Equals(fixture.Id)));
        if (round is null)
        {
            return false;
        }

        var tieFormat = Stage.ResolveEffectiveTieFormat(round, stage.Regulation);
        if (tieFormat.NumberOfLegs != TieFormat.TwoLegs || !tieFormat.AggregateScoring)
        {
            return false;
        }

        if (tieFormat.PenaltyShootoutRule is null)
        {
            return false;
        }

        var expectedLegs = CupConfrontationMaterializer.ExpectedLegsForFixture(stage, fixture);
        if (expectedLegs != TieFormat.TwoLegs)
        {
            return false;
        }

        var legIndex = fixture.Attachments.Single(attachment => attachment.MatchId.Equals(match.Id)).LegIndex;
        if (legIndex != expectedLegs)
        {
            return false;
        }

        var firstLegAttachment = fixture.Attachments.Single(a => a.LegIndex == 1);
        var firstLeg = await matches.GetByIdReadOnlyAsync(firstLegAttachment.MatchId, cancellationToken)
                           .ConfigureAwait(false)
                       ?? throw new InvalidOperationException(
                           $"First leg match '{firstLegAttachment.MatchId}' was not found.");

        if (firstLeg.Status != MatchStatus.Finished || firstLeg.Result?.Score is null)
        {
            return false;
        }

        var proposedShootout = new PenaltyShootoutScore(
            entry.PenaltyShootoutHomeGoals.Value,
            entry.PenaltyShootoutAwayGoals.Value);

        var legs = BuildLegSnapshots(
            fixture,
            match,
            playScore,
            entry.ExtraTimePlayed,
            proposedShootout,
            firstLeg);
        var (entryA, entryB) = ResolveEntryPair(legs);
        return TwoLegAggregateStillLevel.AfterPlayScores(tieFormat, entryA, entryB, legs);
    }

    private static List<FixtureLegSnapshot> BuildLegSnapshots(
        Fixture fixture,
        Match currentMatch,
        Score proposedPlayScore,
        bool proposedExtraTimePlayed,
        PenaltyShootoutScore proposedShootout,
        Match firstLeg)
    {
        var snapshots = new List<FixtureLegSnapshot>(fixture.Attachments.Count);
        foreach (var attachment in fixture.Attachments.OrderBy(a => a.LegIndex))
        {
            if (attachment.MatchId.Equals(currentMatch.Id))
            {
                snapshots.Add(new FixtureLegSnapshot(
                    attachment.LegIndex,
                    currentMatch.Id,
                    currentMatch.HomeEntryId,
                    currentMatch.AwayEntryId,
                    MatchStatus.Finished,
                    proposedPlayScore,
                    proposedExtraTimePlayed,
                    proposedShootout));
                continue;
            }

            if (attachment.LegIndex == 1)
            {
                var firstResult = firstLeg.Result!;
                snapshots.Add(new FixtureLegSnapshot(
                    attachment.LegIndex,
                    firstLeg.Id,
                    firstLeg.HomeEntryId,
                    firstLeg.AwayEntryId,
                    firstLeg.Status,
                    firstResult.Score,
                    firstResult.ExtraTimePlayed,
                    firstResult.PenaltyShootoutScore));
                continue;
            }

            throw new InvalidOperationException(
                $"Unexpected leg attachment for confrontation tie eligibility on leg {attachment.LegIndex}.");
        }

        return snapshots;
    }

    private static (EntryId EntryA, EntryId EntryB) ResolveEntryPair(IReadOnlyList<FixtureLegSnapshot> legs)
    {
        var entries = legs
            .SelectMany(l => new[] { l.HomeEntryId, l.AwayEntryId })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        return entries.Length != 2
            ? throw new DomainException(
                "Fixture confrontation requires exactly two distinct entries across all legs.",
                MatchErrorCodes.InvalidResult)
            : (entries[0], entries[1]);
    }
}
