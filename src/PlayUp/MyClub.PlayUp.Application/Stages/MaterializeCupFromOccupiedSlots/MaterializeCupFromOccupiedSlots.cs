// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlots.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: materialize Cup Fixtures + Matches from occupied bracket Slots (Lot C2).
/// </summary>
/// <remarks>
/// Progression only fills Slot.EntryId. This UC creates Fixture(SlotA/B) and Matches when both
/// slots are occupied. Reuses C1 leg rules via <see cref="CupConfrontationMaterializer"/>.
/// Does not author ProgressionRules, create Stages, or schedule.
/// </remarks>
public static class MaterializeCupFromOccupiedSlots
{
    /// <summary>
    /// Materializes confrontations for the given slot pairs on a Cup stage.
    /// </summary>
    public static MaterializeCupFromOccupiedSlotsResult Execute(
        Competition competition,
        Stage stage,
        IReadOnlyList<CupSlotPair> slotPairs,
        IReadOnlyList<Match> existingMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(slotPairs);
        ArgumentNullException.ThrowIfNull(existingMatches);
        ArgumentNullException.ThrowIfNull(clock);

        if (!stage.CompetitionId.Equals(competition.Id))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' does not belong to competition '{competition.Id}'.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        EnsureMutable(competition, stage);

        if (stage.Rounds.Count == 0 || stage.Matchdays.Count > 0 || stage.Groups.Count > 0)
        {
            throw new ApplicationFailureException(
                "MaterializeCupFromOccupiedSlots requires a Cup stage (rounds only).",
                ApplicationErrorCodes.MaterializationFailure);
        }

        if (slotPairs.Count == 0)
        {
            throw new ApplicationFailureException(
                "At least one slot pair is required.",
                ApplicationErrorCodes.MaterializationFailure);
        }

        var round = stage.Rounds[0];
        var expectedLegs = CupConfrontationMaterializer.ExpectedLegsForRound(round);
        ValidateSlotPairs(slotPairs);

        var knownById = existingMatches
            .Where(match => stage.HasMatch(match.Id))
            .ToDictionary(match => match.Id);

        var resolved = new List<(CupSlotPair Pair, Fixture Fixture, EntryId Home, EntryId Away)>(slotPairs.Count);
        foreach (var pair in slotPairs)
        {
            var slotA = stage.FindSlot(pair.SlotAKey)
                        ?? throw new ApplicationFailureException(
                            $"Slot '{pair.SlotAKey}' was not found.",
                            ApplicationErrorCodes.MaterializationFailure);
            var slotB = stage.FindSlot(pair.SlotBKey)
                        ?? throw new ApplicationFailureException(
                            $"Slot '{pair.SlotBKey}' was not found.",
                            ApplicationErrorCodes.MaterializationFailure);

            if (slotA.EntryId is null || slotB.EntryId is null)
            {
                throw new ApplicationFailureException(
                    $"Both slots must be occupied before materialization ('{pair.SlotAKey}', '{pair.SlotBKey}').",
                    ApplicationErrorCodes.MaterializationFailure);
            }

            if (slotA.EntryId.Equals(slotB.EntryId))
            {
                throw new ApplicationFailureException(
                    $"Slot pair ('{pair.SlotAKey}', '{pair.SlotBKey}') resolves to the same entry.",
                    ApplicationErrorCodes.MaterializationFailure);
            }

            var fixture = FindFixtureForSlots(round, pair.SlotAKey, pair.SlotBKey)
                          ?? stage.AddFixture(round.Id, clock, pair.SlotAKey, pair.SlotBKey);
            resolved.Add((pair, fixture, slotA.EntryId.Value, slotB.EntryId.Value));
        }

        var matchedIndexes = new HashSet<int>();
        var matchedMatchIds = new HashSet<MatchId>();
        for (var i = 0; i < resolved.Count; i++)
        {
            var item = resolved[i];
            if (!CupConfrontationMaterializer.TryMatchCompleteLegs(
                    item.Fixture,
                    item.Home,
                    item.Away,
                    knownById,
                    expectedLegs,
                    out var matchedIds))
            {
                continue;
            }

            matchedIndexes.Add(i);
            foreach (var matchId in matchedIds)
            {
                matchedMatchIds.Add(matchId);
            }
        }

        var allMatched = matchedIndexes.Count == resolved.Count;
        var noneMatched = matchedIndexes.Count == 0;
        var anyAttachments = resolved.Any(item => item.Fixture.MatchIds.Count > 0);
        var targetMatchIds = resolved.SelectMany(item => item.Fixture.MatchIds).ToHashSet();
        var unmatchedOnTargets = existingMatches
            .Where(match => targetMatchIds.Contains(match.Id) && !matchedMatchIds.Contains(match.Id))
            .ToArray();

        if (allMatched && unmatchedOnTargets.Length == 0)
        {
            return new MaterializeCupFromOccupiedSlotsResult(
                [],
                [.. matchedMatchIds],
                AlreadyComplete: true);
        }

        if (!noneMatched || unmatchedOnTargets.Length > 0 || anyAttachments)
        {
            throw new ApplicationFailureException(
                "MaterializeCupFromOccupiedSlots requires empty target fixtures or an exact complete match (partial/divergent state is rejected).",
                ApplicationErrorCodes.MaterializationFailure);
        }

        var created = new List<Match>();
        var attached = new List<MatchId>();
        foreach (var legs in resolved.Select(item => CupConfrontationMaterializer.AttachLegs(
                     stage,
                     item.Fixture.Id,
                     item.Home,
                     item.Away,
                     expectedLegs,
                     clock)))
        {
            created.AddRange(legs);
            attached.AddRange(legs.Select(match => match.Id));
        }

        return new MaterializeCupFromOccupiedSlotsResult(created, attached, AlreadyComplete: false);
    }

    private static void ValidateSlotPairs(IReadOnlyList<CupSlotPair> slotPairs)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in slotPairs)
        {
            if (string.IsNullOrWhiteSpace(pair.SlotAKey) || string.IsNullOrWhiteSpace(pair.SlotBKey))
            {
                throw new ApplicationFailureException(
                    "Slot keys must be non-empty.",
                    ApplicationErrorCodes.MaterializationFailure);
            }

            if (string.Equals(pair.SlotAKey, pair.SlotBKey, StringComparison.Ordinal))
            {
                throw new ApplicationFailureException(
                    $"Slot pair cannot reference the same key twice ('{pair.SlotAKey}').",
                    ApplicationErrorCodes.MaterializationFailure);
            }

            if (!seen.Add(pair.SlotAKey) || !seen.Add(pair.SlotBKey))
            {
                throw new ApplicationFailureException(
                    "Each slot key may appear in at most one pair in the batch.",
                    ApplicationErrorCodes.MaterializationFailure);
            }
        }
    }

    private static Fixture? FindFixtureForSlots(Round round, string slotAKey, string slotBKey) =>
        round.Fixtures.FirstOrDefault(fixture =>
            string.Equals(fixture.SlotAKey, slotAKey, StringComparison.Ordinal)
            && string.Equals(fixture.SlotBKey, slotBKey, StringComparison.Ordinal));

    /// <summary>
    /// Late materialization of a not-yet-started Cup stage is allowed while the competition runs.
    /// Does not unlock StructureLocked: a Running stage remains immutable.
    /// </summary>
    private static void EnsureMutable(Competition competition, Stage stage)
    {
        if (competition.Status is CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Matches cannot be materialized while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Matches cannot be materialized while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }
    }
}
