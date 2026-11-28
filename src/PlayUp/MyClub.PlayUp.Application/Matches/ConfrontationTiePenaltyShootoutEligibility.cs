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

        var firstResult = firstLeg.Result
                           ?? throw new DomainException(
                               "Confrontation tie penalty shootout requires a finished first leg with a score.",
                               MatchErrorCodes.InvalidResult);

        var firstHome = firstLeg.HomeEntryId
                        ?? throw new DomainException(
                            "Confrontation tie penalty shootout requires resolved first-leg entries.",
                            MatchErrorCodes.InvalidResult);
        var firstAway = firstLeg.AwayEntryId
                        ?? throw new DomainException(
                            "Confrontation tie penalty shootout requires resolved first-leg entries.",
                            MatchErrorCodes.InvalidResult);
        var returnHome = match.HomeEntryId
                         ?? throw new DomainException(
                             "Confrontation tie penalty shootout requires resolved return-leg entries.",
                             MatchErrorCodes.InvalidResult);
        var returnAway = match.AwayEntryId
                         ?? throw new DomainException(
                             "Confrontation tie penalty shootout requires resolved return-leg entries.",
                             MatchErrorCodes.InvalidResult);

        return MatchResult.CreateConfrontationTiePenaltyShootout(
            tieFormat,
            firstHome,
            firstAway,
            firstLeg.Status,
            firstResult.Score,
            firstResult.ExtraTimePlayed,
            returnHome,
            returnAway,
            shape.Score,
            shape.ExtraTimePlayed,
            shootout,
            firstLeg.Id,
            match.Id);
    }
}
