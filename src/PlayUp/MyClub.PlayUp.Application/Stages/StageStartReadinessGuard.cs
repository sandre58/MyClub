// -----------------------------------------------------------------------
// <copyright file="StageStartReadinessGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application guard: Ready → Running Start readiness (phase V2 § H).
/// </summary>
internal static class StageStartReadinessGuard
{
    /// <summary>Read-side disabled reason when eligible confrontations remain unmaterialized.</summary>
    internal const string DisabledUnmaterializedConfrontations = "UnmaterializedConfrontations";

    private const string DisabledInitialRoundNotPlayable = "InitialRoundNotPlayable";

    internal static void EnsureReadyToStart(
        Stage stage,
        Competition competition,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        if (StageConfrontationMaterializationState.CountEligibleUnmaterialized(competition, stage, matchSummaries) > 0)
        {
            throw new ApplicationFailureException(
                "Start is blocked because eligible confrontations are not fully materialized.",
                ApplicationErrorCodes.StartBlockedUnmaterialized);
        }
    }

    internal static string? TryGetDisabledReasonCode(
        Stage stage,
        Competition competition,
        IReadOnlyList<MatchSummaryRow> matchSummaries)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(matchSummaries);

        if (StageConfrontationMaterializationState.CountEligibleUnmaterialized(competition, stage, matchSummaries) > 0)
        {
            return DisabledUnmaterializedConfrontations;
        }

        var format = StructureSkeleton.InferFormat(stage);
        if (format == StructureFormatKind.Cup && !IsInitialRoundPlayable(stage))
        {
            return DisabledInitialRoundNotPlayable;
        }

        return null;
    }

    internal static IReadOnlyList<MatchSummaryRow> ToSummaryRows(IReadOnlyList<Match> stageMatches) =>
        stageMatches
            .Select(match => new MatchSummaryRow(
                match.Id,
                match.StageId,
                match.Status,
                match.HomeEntryId,
                match.AwayEntryId,
                match.Result,
                match.HasObservedLive))
            .ToArray();

    private static bool IsInitialRoundPlayable(Stage stage)
    {
        if (stage.Rounds.Count == 0)
        {
            return true;
        }

        var hasSlottedFixtures = stage.Rounds
            .SelectMany(round => round.Fixtures)
            .Any(fixture => fixture.SlotAKey is not null || fixture.SlotBKey is not null);
        if (!hasSlottedFixtures)
        {
            return true;
        }

        var initialRound = stage.Rounds[0];
        return initialRound.Fixtures.Any(fixture => IsFixturePlayable(stage, fixture));
    }

    private static bool IsFixturePlayable(Stage stage, Fixture fixture)
    {
        if (fixture.SlotAKey is null || fixture.SlotBKey is null)
        {
            return false;
        }

        var slotA = stage.Slots.FirstOrDefault(slot => slot.SlotKey == fixture.SlotAKey);
        var slotB = stage.Slots.FirstOrDefault(slot => slot.SlotKey == fixture.SlotBKey);
        return slotA?.EntryId is not null && slotB?.EntryId is not null;
    }
}
