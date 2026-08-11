// -----------------------------------------------------------------------
// <copyright file="ApplyQualification.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;
using StandingView = MyClub.PlayUp.Domain.Standing.Standing;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Application use case: apply qualification paths from standings onto destination slots.
/// </summary>
/// <remarks>
/// Preflights destination stages/slots before any mutation.
/// Replace local only — no cascade. V1: one path → one entry → one slot.
/// Resolves each <see cref="QualificationPath.Source"/> to an already-calculated standing:
/// Overall (or implied overall) uses <c>overallStanding</c>; Group uses the matching entry in
/// <c>groupStandings</c>. Standings are calculated upstream (Application CalculateStanding);
/// this use case does not compute rankings. <see cref="QualificationApplier"/> stays pure:
/// Standing + Path → instruction; it does not load groups.
/// </remarks>
public static class ApplyQualification
{
    /// <summary>
    /// Applies all qualification paths of the source stage using an already-calculated overall standing.
    /// </summary>
    /// <param name="sourceStage">Stage that owns <see cref="QualificationRules"/>.</param>
    /// <param name="standing">Overall standing for paths with overall (or implied overall) source.</param>
    /// <param name="competitionStages">All competition stages (canonical instances for mutations).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Applied slot assignment instructions; empty when no qualification rules.</returns>
    /// <remarks>
    /// Group-scoped paths require the multi-standing overload with per-group standings.
    /// </remarks>
    public static IReadOnlyList<SlotAssignmentInstruction> Execute(
        StageAggregate sourceStage,
        StandingView standing,
        IReadOnlyList<StageAggregate> competitionStages,
        IClock clock) =>
        Execute(
            sourceStage,
            standing,
            new Dictionary<GroupId, StandingView>(),
            competitionStages,
            clock);

    /// <summary>
    /// Applies all qualification paths, resolving Overall and Group sources to the supplied standings.
    /// </summary>
    /// <param name="sourceStage">Stage that owns <see cref="QualificationRules"/>.</param>
    /// <param name="overallStanding">
    /// Standing for overall (or implied overall) paths; required when any such path exists.
    /// </param>
    /// <param name="groupStandings">
    /// Per-group standings keyed by <see cref="GroupId"/>; required for each group-scoped path.
    /// </param>
    /// <param name="competitionStages">All competition stages (canonical instances for mutations).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Applied slot assignment instructions; empty when no qualification rules.</returns>
    public static IReadOnlyList<SlotAssignmentInstruction> Execute(
        StageAggregate sourceStage,
        StandingView? overallStanding,
        IReadOnlyDictionary<GroupId, StandingView> groupStandings,
        IReadOnlyList<StageAggregate> competitionStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(sourceStage);
        ArgumentNullException.ThrowIfNull(groupStandings);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);

        var canonicalSource = ResolveCanonicalStage(sourceStage.Id, competitionStages);
        var paths = canonicalSource.Regulation.QualificationRules?.Paths.ToArray() ?? [];
        if (paths.Length == 0)
        {
            return [];
        }

        var instructions = paths
            .Select(path => QualificationApplier.Apply(path, ResolveStanding(canonicalSource, path, overallStanding, groupStandings)))
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

    private static StandingView ResolveStanding(
        StageAggregate sourceStage,
        QualificationPath path,
        StandingView? overallStanding,
        IReadOnlyDictionary<GroupId, StandingView> groupStandings)
    {
        if (!IsGroupScoped(path))
        {
            return overallStanding
                   ?? throw new ApplicationFailureException(
                       "No overall standing was provided for an overall qualification path.",
                       ApplicationErrorCodes.QualificationStandingMissing);
        }

        var groupId = path.Source.GroupId
                      ?? throw new ApplicationFailureException(
                          "Group-scoped qualification path is missing a group identity.",
                          ApplicationErrorCodes.QualificationGroupNotFound);

        return sourceStage.FindGroup(groupId) is null
            ? throw new ApplicationFailureException(
                $"Qualification path references group '{groupId}' which was not found on stage '{sourceStage.Id}'.",
                ApplicationErrorCodes.QualificationGroupNotFound)
            : !groupStandings.TryGetValue(groupId, out var groupStanding)
                ? throw new ApplicationFailureException(
                    $"No standing was provided for qualification group '{groupId}'.",
                    ApplicationErrorCodes.QualificationStandingMissing)
                : groupStanding;
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

    private static bool IsGroupScoped(QualificationPath path) =>
        path.Source.GroupId is not null || path.Source.Scope == RankingScope.Group;
}
