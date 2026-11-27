// -----------------------------------------------------------------------
// <copyright file="NonFinalLegEtTabEntryGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Refuses match results that record extra time or a penalty shootout on a non-final confrontation leg.
/// </summary>
internal static class NonFinalLegEtTabEntryGuard
{
    /// <summary>
    /// Ensures <paramref name="result"/> may be entered for <paramref name="matchId"/> on <paramref name="stage"/>.
    /// </summary>
    /// <param name="stage">Stage that may attach the match to a fixture.</param>
    /// <param name="matchId">Match being finished or corrected.</param>
    /// <param name="result">Proposed result.</param>
    public static void EnsureAllowed(Stage stage, MatchId matchId, MatchResult result)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(result);

        if (!result.ExtraTimePlayed && result.PenaltyShootoutScore is null)
        {
            return;
        }

        var fixtureId = stage.FindFixtureIdContainingMatch(matchId);
        if (fixtureId is null)
        {
            return;
        }

        var fixture = stage.FindFixture(fixtureId.Value)
                      ?? throw new InvalidOperationException(
                          $"Fixture '{fixtureId}' was reported for match '{matchId}' but is missing on stage '{stage.Id}'.");

        var legIndex = fixture.Attachments.Single(attachment => attachment.MatchId.Equals(matchId)).LegIndex;
        var expectedLegs = CupConfrontationMaterializer.ExpectedLegsForFixture(stage, fixture);
        if (legIndex < expectedLegs)
        {
            throw new DomainException(
                $"Extra time or penalty shootout is not allowed on non-final leg {legIndex}.",
                MatchErrorCodes.ExtraTimeOrShootoutOnNonFinalLeg);
        }
    }
}
