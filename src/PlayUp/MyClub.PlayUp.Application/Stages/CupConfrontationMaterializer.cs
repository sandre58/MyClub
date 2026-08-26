// -----------------------------------------------------------------------
// <copyright file="CupConfrontationMaterializer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Shared Application helper: attach cup confrontation Matches (LegIndex 1 / optional 2) to a Fixture.
/// </summary>
/// <remarks>
/// Used by Pairing <see cref="ApplyDraw"/> and <see cref="MaterializeCupFromOccupiedSlots"/>.
/// TwoLegs = same Fixture, Leg 2 Home/Away mirrored — not Double RR.
/// </remarks>
internal static class CupConfrontationMaterializer
{
    /// <summary>
    /// Resolves expected leg count from the Round hosting the fixture (null TieFormat ⇒ OneLeg).
    /// </summary>
    public static int ExpectedLegsForFixture(Stage stage, Fixture fixture)
    {
        var round = stage.Rounds.FirstOrDefault(r => r.Fixtures.Any(f => f.Id.Equals(fixture.Id)));
        return TieFormat.OrDefaultOneLeg(round?.TieFormat).NumberOfLegs;
    }

    /// <summary>
    /// Resolves expected leg count for a Round (null TieFormat ⇒ OneLeg).
    /// </summary>
    public static int ExpectedLegsForRound(Round round) =>
        TieFormat.OrDefaultOneLeg(round.TieFormat).NumberOfLegs;

    /// <summary>
    /// Creates and attaches Leg 1 (and Leg 2 when <paramref name="expectedLegs"/> is 2).
    /// </summary>
    public static IReadOnlyList<Match> AttachLegs(
        Stage stage,
        FixtureId fixtureId,
        EntryId home,
        EntryId away,
        int expectedLegs,
        IClock clock)
    {
        var created = new List<Match>(expectedLegs);
        var leg1 = Match.Create(stage.CompetitionId, stage.Id, home, away, clock);
        stage.AttachMatch(fixtureId, leg1.Id, legIndex: 1, clock);
        created.Add(leg1);

        if (expectedLegs == TieFormat.TwoLegs)
        {
            var leg2 = Match.Create(stage.CompetitionId, stage.Id, away, home, clock);
            stage.AttachMatch(fixtureId, leg2.Id, legIndex: 2, clock);
            created.Add(leg2);
        }

        return created;
    }

    /// <summary>
    /// Returns whether the fixture already has the complete expected legs for home/away (and mirror).
    /// </summary>
    public static bool TryMatchCompleteLegs(
        Fixture fixture,
        EntryId home,
        EntryId away,
        IReadOnlyDictionary<MatchId, Match> knownById,
        int expectedLegs,
        out List<MatchId> matchedIds)
    {
        matchedIds = [];
        if (fixture.Attachments.Count != expectedLegs)
        {
            return false;
        }

        var leg1 = FindAttachedMatch(fixture, knownById, legIndex: 1, home, away);
        if (leg1 is null)
        {
            return false;
        }

        matchedIds.Add(leg1.Id);
        if (expectedLegs == TieFormat.SingleLeg)
        {
            return true;
        }

        var leg2 = FindAttachedMatch(fixture, knownById, legIndex: 2, away, home);
        if (leg2 is null)
        {
            return false;
        }

        matchedIds.Add(leg2.Id);
        return true;
    }

    /// <summary>
    /// Finds an attached match at the given leg with exact home/away.
    /// </summary>
    public static Match? FindAttachedMatch(
        Fixture fixture,
        IReadOnlyDictionary<MatchId, Match> knownById,
        int legIndex,
        EntryId home,
        EntryId away)
    {
        var attachment = fixture.Attachments.FirstOrDefault(a => a.LegIndex == legIndex);
        if (attachment is null || !knownById.TryGetValue(attachment.MatchId, out var match))
        {
            return null;
        }

        return match.HomeEntryId.Equals(home) && match.AwayEntryId.Equals(away)
            ? match
            : null;
    }
}
