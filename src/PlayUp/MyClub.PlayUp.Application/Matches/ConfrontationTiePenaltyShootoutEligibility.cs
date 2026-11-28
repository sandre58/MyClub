// -----------------------------------------------------------------------
// <copyright file="ConfrontationTiePenaltyShootoutEligibility.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Allows a decisive penalty shootout on an unequal play score when the confrontation aggregate is tied
/// after the final leg (two-legged ties).
/// </summary>
internal static class ConfrontationTiePenaltyShootoutEligibility
{
    public static bool AllowsUnequalPlayScore(
        Stage stage,
        Match currentMatch,
        Score proposedScore,
        bool extraTimePlayed,
        PenaltyShootoutScore? shootout,
        IReadOnlyList<Match> stageMatches)
    {
        ArgumentNullException.ThrowIfNull(currentMatch);

        if (shootout is null || proposedScore.HomeGoals == proposedScore.AwayGoals)
        {
            return false;
        }

        if (currentMatch.HomeEntryId is null || currentMatch.AwayEntryId is null)
        {
            return false;
        }

        var firstLeg = FindMirroredFinishedLeg(currentMatch, stageMatches);
        if (firstLeg?.Result?.Score is not { } firstScore)
        {
            return false;
        }

        var tieFormat = ResolveTieFormat(stage, currentMatch.Id);
        if (tieFormat.NumberOfLegs < TieFormat.TwoLegs || !tieFormat.AggregateScoring)
        {
            tieFormat = new TieFormat(
                TieFormat.TwoLegs,
                aggregateScoring: true,
                tieFormat.AwayGoalsRule,
                tieFormat.ExtraTimeRule,
                tieFormat.PenaltyShootoutRule);
        }

        return AggregateIsTied(
            firstLeg.HomeEntryId!.Value,
            firstLeg.AwayEntryId!.Value,
            firstScore,
            currentMatch.HomeEntryId!.Value,
            currentMatch.AwayEntryId!.Value,
            proposedScore,
            tieFormat);
    }

    private static TieFormat ResolveTieFormat(Stage stage, MatchId matchId)
    {
        var fixtureId = stage.FindFixtureIdContainingMatch(matchId);
        if (fixtureId is not { } resolvedFixtureId)
        {
            return Stage.ResolveEffectiveDefault(stage.Regulation);
        }

        var round = stage.Rounds.FirstOrDefault(candidate =>
            candidate.Fixtures.Any(f => f.Id.Equals(resolvedFixtureId)));
        return round is not null
            ? Stage.ResolveEffectiveTieFormat(round, stage.Regulation)
            : Stage.ResolveEffectiveDefault(stage.Regulation);
    }

    private static Match? FindMirroredFinishedLeg(Match current, IReadOnlyList<Match> stageMatches)
    {
        if (current.HomeEntryId is null || current.AwayEntryId is null)
        {
            return null;
        }

        var confrontation = new HashSet<EntryId> { current.HomeEntryId.Value, current.AwayEntryId.Value };
        return stageMatches.FirstOrDefault(candidate =>
            !candidate.Id.Equals(current.Id)
            && candidate.Result is not null
            && candidate.HomeEntryId is not null
            && candidate.AwayEntryId is not null
            && confrontation.SetEquals([candidate.HomeEntryId.Value, candidate.AwayEntryId.Value]));
    }

    private static bool AggregateIsTied(
        EntryId firstHome,
        EntryId firstAway,
        Score firstScore,
        EntryId secondHome,
        EntryId secondAway,
        Score secondScore,
        TieFormat tieFormat)
    {
        var entryA = firstHome;
        var entryB = firstAway;
        if (!secondHome.Equals(entryA) && !secondHome.Equals(entryB))
        {
            return false;
        }

        var totalA = 0;
        var totalB = 0;
        var awayA = 0;
        var awayB = 0;

        AddGoals(firstHome, firstScore.HomeGoals, isAway: false, entryA, entryB, ref totalA, ref totalB, ref awayA, ref awayB);
        AddGoals(firstAway, firstScore.AwayGoals, isAway: true, entryA, entryB, ref totalA, ref totalB, ref awayA, ref awayB);
        AddGoals(secondHome, secondScore.HomeGoals, isAway: false, entryA, entryB, ref totalA, ref totalB, ref awayA, ref awayB);
        AddGoals(secondAway, secondScore.AwayGoals, isAway: true, entryA, entryB, ref totalA, ref totalB, ref awayA, ref awayB);

        if (totalA != totalB)
        {
            return false;
        }

        if (tieFormat.AwayGoalsRule is null)
        {
            return true;
        }

        return awayA == awayB;
    }

    private static void AddGoals(
        EntryId scorer,
        int goals,
        bool isAway,
        EntryId entryA,
        EntryId entryB,
        ref int totalA,
        ref int totalB,
        ref int awayA,
        ref int awayB)
    {
        if (scorer.Equals(entryA))
        {
            totalA += goals;
            if (isAway)
            {
                awayA += goals;
            }
        }
        else if (scorer.Equals(entryB))
        {
            totalB += goals;
            if (isAway)
            {
                awayB += goals;
            }
        }
    }
}
