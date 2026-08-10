// -----------------------------------------------------------------------
// <copyright file="ApplyQualification.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Stage;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;
using StandingView = MyClub.PlayUp.Domain.Standing.Standing;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Application use case: apply qualification paths from a standing onto destination slots.
/// </summary>
/// <remarks>
/// Preflights destination stages/slots before any mutation.
/// Replace local only — no cascade. V1: one path → one entry → one slot.
/// </remarks>
public static class ApplyQualification
{
    /// <summary>
    /// Applies all qualification paths of the source stage using an already-calculated standing.
    /// </summary>
    /// <param name="sourceStage">Stage that owns <see cref="Domain.Rules.QualificationRules"/>.</param>
    /// <param name="standing">Standing calculated for the path sources.</param>
    /// <param name="competitionStages">All competition stages (canonical instances for mutations).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Applied slot assignment instructions; empty when no qualification rules.</returns>
    public static IReadOnlyList<SlotAssignmentInstruction> Execute(
        StageAggregate sourceStage,
        StandingView standing,
        IReadOnlyList<StageAggregate> competitionStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(sourceStage);
        ArgumentNullException.ThrowIfNull(standing);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);

        var canonicalSource = ResolveCanonicalStage(sourceStage.Id, competitionStages);
        var paths = canonicalSource.Regulation.QualificationRules?.Paths.ToArray() ?? [];
        if (paths.Length == 0)
        {
            return [];
        }

        var instructions = paths
            .Select(path => QualificationApplier.Apply(path, standing))
            .ToArray();

        var destinations = new StageAggregate[instructions.Length];
        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            var destination = ResolveCanonicalStage(instruction.StageId, competitionStages);
            if (destination.FindSlot(instruction.SlotKey) is null)
            {
                throw new ApplicationFailureException(
                    $"Qualification destination slot '{instruction.SlotKey}' was not found on stage '{destination.Id}'.",
                    ApplicationErrorCodes.DanglingFeedTarget);
            }

            destinations[i] = destination;
        }

        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            destinations[i].ApplyResolvedEntry(instruction.SlotKey, instruction.EntryId, clock);
        }

        return instructions;
    }

    private static StageAggregate ResolveCanonicalStage(
        StageId stageId,
        IReadOnlyList<StageAggregate> competitionStages)
    {
        var stage = competitionStages.FirstOrDefault(s => s.Id.Equals(stageId));
        return stage
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
    }
}
