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
    /// Builds a confrontation-tie flagged result when every eligibility criterion is met; otherwise throws <see cref="MatchErrorCodes.InvalidResult"/>.
    /// </summary>
    /// <remarks>Eligibility on finish/result routes is a precondition for calling this method.</remarks>
    public static async Task<MatchResult> BuildFlaggedResultAsync(
        Stage stage,
        Match match,
        MatchResultEntryShape shape,
        IMatchRepository matches,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(matches);

        if (shape.Type != ResultType.Played)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout is allowed only for played results.",
                MatchErrorCodes.InvalidResult);
        }

        var shootout = shape.Shootout
                        ?? throw new DomainException(
                            "Confrontation tie penalty shootout requires a decisive shootout score.",
                            MatchErrorCodes.InvalidResult);

        if (shape.Score.HomeGoals == shape.Score.AwayGoals)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires an unequal play score on the final leg.",
                MatchErrorCodes.InvalidResult);
        }

        var fixtureId = stage.FindFixtureIdContainingMatch(match.Id);
        if (fixtureId is null)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a round confrontation.",
                MatchErrorCodes.InvalidResult);
        }

        var fixture = stage.FindFixture(fixtureId.Value);
        if (fixture is null)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a valid confrontation fixture.",
                MatchErrorCodes.InvalidResult);
        }

        if (fixture.Attachments.Count != 2)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires exactly two legs on the fixture.",
                MatchErrorCodes.InvalidResult);
        }

        var round = stage.Rounds.FirstOrDefault(r => r.Fixtures.Any(f => f.Id.Equals(fixture.Id)));
        if (round is null)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout is allowed only in a cup round.",
                MatchErrorCodes.InvalidResult);
        }

        var tieFormat = Stage.ResolveEffectiveTieFormat(round, stage.Regulation);
        if (tieFormat.NumberOfLegs != TieFormat.TwoLegs || !tieFormat.AggregateScoring)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a two-leg aggregate format.",
                MatchErrorCodes.InvalidResult);
        }

        if (tieFormat.PenaltyShootoutRule is null)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a penalty shootout rule.",
                MatchErrorCodes.InvalidResult);
        }

        var expectedLegs = CupConfrontationMaterializer.ExpectedLegsForFixture(stage, fixture);
        if (expectedLegs != TieFormat.TwoLegs)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires two expected legs.",
                MatchErrorCodes.InvalidResult);
        }

        var currentAttachment = fixture.Attachments.FirstOrDefault(attachment => attachment.MatchId.Equals(match.Id));
        if (currentAttachment is null)
        {
            throw new DomainException(
                "Match is not attached to the confrontation fixture.",
                MatchErrorCodes.InvalidResult);
        }

        if (currentAttachment.LegIndex != expectedLegs)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout is allowed only on the final leg.",
                MatchErrorCodes.InvalidResult);
        }

        var firstLegAttachment = fixture.Attachments.FirstOrDefault(a => a.LegIndex == 1);
        if (firstLegAttachment is null)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a first leg.",
                MatchErrorCodes.InvalidResult);
        }

        var firstLeg = await matches.GetByIdReadOnlyAsync(firstLegAttachment.MatchId, cancellationToken)
                           .ConfigureAwait(false);
        if (firstLeg is null)
        {
            throw new DomainException(
                "First leg match was not found.",
                MatchErrorCodes.InvalidResult);
        }

        if (firstLeg.Status != MatchStatus.Finished || firstLeg.Result?.Score is null)
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a finished first leg with a score.",
                MatchErrorCodes.InvalidResult);
        }

        var legs = BuildLegSnapshots(
            fixture,
            match,
            shape.Score,
            shape.ExtraTimePlayed,
            shootout,
            firstLeg);
        var (entryA, entryB) = ResolveEntryPair(legs);
        var proof = ConfrontationTieEligibility.CreateProof(tieFormat, entryA, entryB, legs);

        return MatchResult.CreateConfrontationTiePenaltyShootout(shape.Score, shape.ExtraTimePlayed, shootout, proof);
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

            throw new DomainException(
                $"Unexpected leg attachment for confrontation tie eligibility on leg {attachment.LegIndex}.",
                MatchErrorCodes.InvalidResult);
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

        if (entries.Length != 2)
        {
            throw new DomainException(
                "Fixture confrontation requires exactly two distinct entries across all legs.",
                MatchErrorCodes.InvalidResult);
        }

        return (entries[0], entries[1]);
    }
}
