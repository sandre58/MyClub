// -----------------------------------------------------------------------
// <copyright file="ApplyDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Stage;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Application use case: apply a published Draw resolution (Slot, Group, or Pairing) onto its owning Stage.
/// </summary>
/// <remarks>
/// Preflights all known failure conditions before any mutation.
/// V1 Application: preflight → Domain mutations. No unit of work / EF transaction here;
/// persisted atomicity is a future Host/Infrastructure responsibility.
/// Pairing: opposition is conceptually unordered; V1 Match creation maps EntryA→Home, EntryB→Away
/// as a technical convention only. Fixture target is Application orchestration input.
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
    /// <param name="pairingContext">Required for Pairing kind (target Fixture).</param>
    /// <param name="knownMatches">Matches already attached to the target Fixture (for Pairing idempotence).</param>
    /// <returns>Slot instructions and/or newly created Matches.</returns>
    public static ApplyDrawResult Execute(
        StageAggregate stage,
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
        StageAggregate stage,
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
        StageAggregate stage,
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
            stage.AssignEntryToGroup(placement.GroupId, placement.EntryId, clock);
        }

        return [];
    }

    private static List<Match> ApplyPairing(
        StageAggregate stage,
        Draw draw,
        IClock clock,
        PairingApplicationContext? pairingContext,
        IReadOnlyList<Match> knownMatches)
    {
        if (pairingContext is null)
        {
            throw new ApplicationFailureException(
                "Pairing draw apply requires a PairingApplicationContext with a target FixtureId.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Pairing draw cannot be applied when stage structure is locked (status '{stage.Status}').",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var fixture = stage.FindFixture(pairingContext.FixtureId)
            ?? throw new ApplicationFailureException(
                $"Fixture '{pairingContext.FixtureId}' was not found on stage '{stage.Id}'.",
                ApplicationErrorCodes.DrawApplyFailure);

        var pairings = draw.Resolution.PairingResults;
        if (pairings.Count == 0)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' has no pairing results to apply.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

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

        foreach (var match in knownMatches)
        {
            if (!match.StageId.Equals(stage.Id) || !match.CompetitionId.Equals(stage.CompetitionId))
            {
                throw new ApplicationFailureException(
                    $"Known match '{match.Id}' does not belong to stage '{stage.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }

            if (!fixture.MatchIds.Contains(match.Id))
            {
                throw new ApplicationFailureException(
                    $"Known match '{match.Id}' is not attached to fixture '{fixture.Id}'.",
                    ApplicationErrorCodes.DrawApplyFailure);
            }
        }

        if (knownMatches.Count != fixture.MatchIds.Count)
        {
            throw new ApplicationFailureException(
                "knownMatches must include every match already attached to the target fixture.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var fixtureMatchSet = knownMatches.ToList();
        var matchedPairingIndexes = new HashSet<int>();
        var matchedMatchIds = new HashSet<MatchId>();

        for (var i = 0; i < pairings.Count; i++)
        {
            var pairing = pairings[i];
            var existing = fixtureMatchSet.FirstOrDefault(m =>
                m.HomeEntryId.Equals(pairing.EntryA) && m.AwayEntryId.Equals(pairing.EntryB));
            if (existing is null) continue;
            matchedPairingIndexes.Add(i);
            matchedMatchIds.Add(existing.Id);
        }

        var unmatchedMatches = fixtureMatchSet.Where(m => !matchedMatchIds.Contains(m.Id)).ToArray();
        var allMatched = matchedPairingIndexes.Count == pairings.Count;
        var noneMatched = matchedPairingIndexes.Count == 0;

        if (allMatched && unmatchedMatches.Length == 0)
        {
            return [];
        }

        if (!noneMatched || unmatchedMatches.Length > 0 || fixture.MatchIds.Count > 0)
        {
            throw new ApplicationFailureException(
                "Pairing draw apply requires an empty fixture or an exact match of all pairings (partial/divergent state is rejected).",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var created = new List<Match>(pairings.Count);
        var nextLegIndex = 1;
        foreach (var pairing in pairings)
        {
            var match = Match.Create(
                stage.CompetitionId,
                stage.Id,
                pairing.EntryA,
                pairing.EntryB,
                clock);
            stage.AttachMatch(fixture.Id, match.Id, nextLegIndex++, clock);
            created.Add(match);
        }

        return created;
    }
}
