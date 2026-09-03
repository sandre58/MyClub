// -----------------------------------------------------------------------
// <copyright file="ApplyDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: apply a published Draw resolution (Slot, Group, or Pairing) onto its owning Stage.
/// </summary>
/// <remarks>
/// Preflights all known failure conditions before any mutation.
/// Persistence: caller loads tracked ARs in one DI scope, invokes this use case, then calls
/// <c>IUnitOfWork.SaveChangesAsync</c> once. Pairing callers must
/// <c>IMatchRepository.Add</c> each created Match before that single SaveChanges.
/// Pairing: opposition is conceptually unordered; V1 Match creation maps EntryA→Home, EntryB→Away
/// as a technical convention for LegIndex 1. When the hosting Round has <c>TieFormat.NumberOfLegs == 2</c>,
/// LegIndex 2 is created on the same Fixture with Home/Away mirrored (B→A). Null TieFormat ⇒ one leg.
/// Fixture targets are Application orchestration input (one Fixture per pairing).
/// Does not recalculate WhoFeeds and never creates DirectAssignment.
/// Host supplies Pairing fixture context and Draw entry pools (typically ⊆ qualified/progressed Entries).
/// </remarks>
public static class ApplyDraw
{
    /// <summary>
    /// Applies a published resolved Draw onto already-loaded Stage.
    /// </summary>
    /// <param name="stage">Stage that owns the Draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <param name="pairingContext">Required for Pairing kind (target Fixtures, 1:1 with pairings).</param>
    /// <param name="knownMatches">Matches already attached to any target Fixture (for Pairing idempotence).</param>
    /// <returns>Slot instructions and/or newly created Matches.</returns>
    public static ApplyDrawResult Execute(
        Stage stage,
        DrawId drawId,
        IClock clock,
        PairingApplicationContext? pairingContext = null,
        IReadOnlyList<Match>? knownMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        var draw = stage.GetDraw(drawId);
        EnsureApplicable(draw);

        return draw.Kind switch
        {
            DrawResolutionKind.Slot => new ApplyDrawResult(ApplySlot(stage, draw, clock), []),
            DrawResolutionKind.Group => new ApplyDrawResult(ApplyGroup(stage, draw, clock), []),
            DrawResolutionKind.Pairing => new ApplyDrawResult(
                [],
                ApplyPairing(stage, draw, clock, pairingContext, knownMatches ?? [])),
            _ => throw new ApplicationFailureException(
                $"ApplyDraw does not support draw kind '{draw.Kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static void EnsureApplicable(Draw draw)
    {
        if (draw.Status != DrawStatus.Published)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' must be Published to apply (status is '{draw.Status}').",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' must be Resolved to apply (resolution is '{draw.Resolution.State}').",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (draw.Resolution.ResolvedKind != draw.Kind)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' resolution kind does not match draw kind.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (draw.Inputs is null)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' has no configured inputs.",
                ApplicationErrorCodes.DrawApplyFailure);
        }
    }

    private static IReadOnlyList<SlotAssignmentInstruction> ApplySlot(
        Stage stage,
        Draw draw,
        IClock clock)
    {
        var instructions = draw.ToSlotAssignmentInstructions(stage.Id);
        var pool = draw.Inputs!.Entries;

        foreach (var instruction in instructions)
        {
            if (!instruction.StageId.Equals(stage.Id))
            {
                throw new ApplicationFailureException(
                    $"Slot instruction targets stage '{instruction.StageId}' but draw belongs to '{stage.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            var slot = stage.FindSlot(instruction.SlotKey)
                ?? throw new ApplicationFailureException(
                    $"Draw slot '{instruction.SlotKey}' was not found on stage '{stage.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);

            if (!pool.Contains(instruction.EntryId))
            {
                throw new ApplicationFailureException(
                    $"Entry '{instruction.EntryId}' is outside the draw pool.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            if (stage.DirectAssignments.Any(a =>
                    string.Equals(a.SlotKey, slot.SlotKey, StringComparison.Ordinal)))
            {
                throw new ApplicationFailureException(
                    $"Slot '{slot.SlotKey}' is owned by a direct assignment and cannot receive a draw resolution.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            if (slot.EntryId is { } current && !current.Equals(instruction.EntryId))
            {
                throw new ApplicationFailureException(
                    $"Slot '{slot.SlotKey}' already occupied by '{current}'; draw requires '{instruction.EntryId}'.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }
        }

        foreach (var instruction in instructions)
        {
            stage.ApplyResolvedEntry(instruction.SlotKey, instruction.EntryId, clock);
        }

        return instructions;
    }

    private static IReadOnlyList<SlotAssignmentInstruction> ApplyGroup(
        Stage stage,
        Draw draw,
        IClock clock)
    {
        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Group draw cannot be applied when stage structure is locked (status '{stage.Status}').",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var placements = draw.Resolution.GroupResults;
        var pool = draw.Inputs!.Entries;

        foreach (var placement in placements)
        {
            _ = stage.FindGroup(placement.GroupId)
                ?? throw new ApplicationFailureException(
                    $"Draw group '{placement.GroupId}' was not found on stage '{stage.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);

            if (!pool.Contains(placement.EntryId))
            {
                throw new ApplicationFailureException(
                    $"Entry '{placement.EntryId}' is outside the draw pool.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            if (stage.Groups.FirstOrDefault(g => g.EntryIds.Contains(placement.EntryId)) is { } owningGroup
                && !owningGroup.Id.Equals(placement.GroupId))
            {
                throw new ApplicationFailureException(
                    $"Entry '{placement.EntryId}' is already assigned to group '{owningGroup.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }
        }

        foreach (var placement in placements)
        {
            stage.AssignEntryToGroup(placement.GroupId, placement.EntryId);
        }

        return [];
    }

    private static List<Match> ApplyPairing(
        Stage stage,
        Draw draw,
        IClock clock,
        PairingApplicationContext? pairingContext,
        IReadOnlyList<Match> knownMatches)
    {
        if (pairingContext is null)
        {
            throw new ApplicationFailureException(
                "Pairing draw apply requires a PairingApplicationContext with target FixtureIds.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Pairing draw cannot be applied when stage structure is locked (status '{stage.Status}').",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var pairings = draw.Resolution.PairingResults;
        if (pairings.Count == 0)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' has no pairing results to apply.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var fixtureIds = pairingContext.FixtureIds;
        if (fixtureIds.Count != pairings.Count)
        {
            throw new ApplicationFailureException(
                $"Pairing draw apply requires exactly one FixtureId per pairing (got {fixtureIds.Count} fixtures for {pairings.Count} pairings).",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (fixtureIds.Distinct().Count() != fixtureIds.Count)
        {
            throw new ApplicationFailureException(
                "Pairing draw apply requires distinct FixtureIds (one fixture per pairing).",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var fixtures = new List<Fixture>(fixtureIds.Count);
        fixtures.AddRange(fixtureIds.Select(fixtureId => stage.FindFixture(fixtureId) ?? throw new ApplicationFailureException($"Fixture '{fixtureId}' was not found on stage '{stage.Id}'.", ApplicationErrorCodes.DrawApplyFailure)));

        var pool = draw.Inputs!.Entries;
        var seenEntries = new HashSet<EntryId>();
        foreach (var pairing in pairings)
        {
            if (!pool.Contains(pairing.EntryA) || !pool.Contains(pairing.EntryB))
            {
                throw new ApplicationFailureException(
                    "Pairing references an entry outside the draw pool.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            if (!seenEntries.Add(pairing.EntryA) || !seenEntries.Add(pairing.EntryB))
            {
                throw new ApplicationFailureException(
                    "An entry participates in more than one pairing in the resolution.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }
        }

        var targetMatchIds = fixtures.SelectMany(f => f.MatchIds).ToHashSet();
        foreach (var match in knownMatches)
        {
            if (!match.StageId.Equals(stage.Id) || !match.CompetitionId.Equals(stage.CompetitionId))
            {
                throw new ApplicationFailureException(
                    $"Known match '{match.Id}' does not belong to stage '{stage.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            if (!targetMatchIds.Contains(match.Id))
            {
                throw new ApplicationFailureException(
                    $"Known match '{match.Id}' is not attached to any of the target fixtures.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }
        }

        if (knownMatches.Count != targetMatchIds.Count)
        {
            throw new ApplicationFailureException(
                "knownMatches must include every match already attached to the target fixtures.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var knownById = knownMatches.ToDictionary(m => m.Id);
        var matchedPairingIndexes = new HashSet<int>();
        var matchedMatchIds = new HashSet<MatchId>();

        for (var i = 0; i < pairings.Count; i++)
        {
            var pairing = pairings[i];
            var fixture = fixtures[i];
            var expectedLegs = CupConfrontationMaterializer.ExpectedLegsForFixture(stage, fixture);
            if (!CupConfrontationMaterializer.TryMatchCompleteLegs(
                    fixture,
                    pairing.EntryA,
                    pairing.EntryB,
                    knownById,
                    expectedLegs,
                    out var matchedIds))
            {
                continue;
            }

            matchedPairingIndexes.Add(i);
            foreach (var matchId in matchedIds)
            {
                matchedMatchIds.Add(matchId);
            }
        }

        var unmatchedMatches = knownMatches.Where(m => !matchedMatchIds.Contains(m.Id)).ToArray();
        var allMatched = matchedPairingIndexes.Count == pairings.Count;
        var noneMatched = matchedPairingIndexes.Count == 0;
        var anyAttachments = fixtures.Any(f => f.MatchIds.Count > 0);

        if (allMatched && unmatchedMatches.Length == 0)
        {
            return [];
        }

        if (!noneMatched || unmatchedMatches.Length > 0 || anyAttachments)
        {
            throw new ApplicationFailureException(
                "Pairing draw apply requires empty target fixtures or an exact match of all pairings (partial/divergent state is rejected).",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var created = new List<Match>();
        for (var i = 0; i < pairings.Count; i++)
        {
            var pairing = pairings[i];
            var fixtureId = fixtureIds[i];
            var expectedLegs = CupConfrontationMaterializer.ExpectedLegsForFixture(stage, fixtures[i]);
            created.AddRange(
                CupConfrontationMaterializer.AttachLegs(
                    stage,
                    fixtureId,
                    pairing.EntryA,
                    pairing.EntryB,
                    expectedLegs,
                    clock));
        }

        return created;
    }
}
