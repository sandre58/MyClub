// -----------------------------------------------------------------------
// <copyright file="ApplyQualification.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: apply qualification paths from standings onto destination slots.
/// </summary>
/// <remarks>
/// Preflights destination stages/slots before any mutation.
/// Replace local only — no cascade. V1: one path → one entry → one slot.
/// Resolves each <see cref="QualificationPath.Source"/> to a standing:
/// Overall uses <c>overallStanding</c>; Group uses <c>groupStandings</c>;
/// AcrossGroups builds a derived standing via <see cref="CrossGroupStandingAssembler"/>
/// (requires matches). <see cref="QualificationApplier"/> stays pure and source-agnostic.
/// Persistence: caller loads all competition Stages into one scope, invokes this use case,
/// then calls <c>IUnitOfWork.SaveChangesAsync</c> once (no Application transaction API).
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
    /// Group-scoped and AcrossGroups paths require the multi-standing overload.
    /// </remarks>
    public static IReadOnlyList<SlotAssignmentInstruction> Execute(
        Stage sourceStage,
        Standing standing,
        IReadOnlyList<Stage> competitionStages,
        IClock clock) =>
        Execute(
            sourceStage,
            standing,
            new Dictionary<GroupId, Standing>(),
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
    /// Per-group standings keyed by <see cref="GroupId"/>; required for each group-scoped path
    /// and for every stage group when AcrossGroups paths exist.
    /// </param>
    /// <param name="competitionStages">All competition stages (canonical instances for mutations).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Applied slot assignment instructions; empty when no qualification rules.</returns>
    /// <remarks>
    /// AcrossGroups paths require the overload that supplies matches.
    /// </remarks>
    public static IReadOnlyList<SlotAssignmentInstruction> Execute(
        Stage sourceStage,
        Standing? overallStanding,
        IReadOnlyDictionary<GroupId, Standing> groupStandings,
        IReadOnlyList<Stage> competitionStages,
        IClock clock) =>
        Execute(
            sourceStage,
            overallStanding,
            groupStandings,
            matches: null,
            competitionStages,
            clock);

    /// <summary>
    /// Applies all qualification paths, including AcrossGroups derived standings when matches are provided.
    /// </summary>
    /// <param name="sourceStage">Stage that owns <see cref="QualificationRules"/>.</param>
    /// <param name="overallStanding">
    /// Standing for overall (or implied overall) paths; required when any such path exists.
    /// </param>
    /// <param name="groupStandings">
    /// Per-group standings keyed by <see cref="GroupId"/>.
    /// </param>
    /// <param name="matches">
    /// Stage matches required to assemble AcrossGroups derived standings; may be null when unused.
    /// </param>
    /// <param name="competitionStages">All competition stages (canonical instances for mutations).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Applied slot assignment instructions; empty when no qualification rules.</returns>
    public static IReadOnlyList<SlotAssignmentInstruction> Execute(
        Stage sourceStage,
        Standing? overallStanding,
        IReadOnlyDictionary<GroupId, Standing> groupStandings,
        IReadOnlyList<Match>? matches,
        IReadOnlyList<Stage> competitionStages,
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

        var derivedCache = new Dictionary<int, Standing>();
        var outcomes = new (QualificationPath Path, SlotAssignmentInstruction? Instruction)[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            var path = paths[i];
            var standing = ResolveStanding(
                canonicalSource,
                path,
                overallStanding,
                groupStandings,
                matches,
                derivedCache);
            outcomes[i] = (path, QualificationApplier.Apply(path, standing));
        }

        var applied = new List<SlotAssignmentInstruction>(outcomes.Length);
        foreach (var (path, instruction) in outcomes)
        {
            var destination = ResolveCanonicalStage(path.Destination.StageId, competitionStages);
            if (destination.FindSlot(path.Destination.SlotKey) is null)
            {
                throw new ApplicationFailureException(
                    $"Qualification destination slot '{path.Destination.SlotKey}' was not found on stage '{destination.Id}'.",
                    ApplicationErrorCodes.DanglingFeedTarget);
            }

            if (instruction is null)
            {
                destination.ClearResolvedEntry(path.Destination.SlotKey, clock);
                continue;
            }

            destination.ApplyResolvedEntry(instruction.SlotKey, instruction.EntryId, clock);
            applied.Add(instruction);
        }

        return applied;
    }

    private static Standing ResolveStanding(
        Stage sourceStage,
        QualificationPath path,
        Standing? overallStanding,
        IReadOnlyDictionary<GroupId, Standing> groupStandings,
        IReadOnlyList<Match>? matches,
        Dictionary<int, Standing> derivedCache)
    {
        if (path.Source.Scope == RankingScope.AcrossGroups)
        {
            if (matches is null)
            {
                throw new ApplicationFailureException(
                    "Across-groups qualification paths require stage matches.",
                    ApplicationErrorCodes.QualificationMatchesRequired);
            }

            var position = path.Source.AcrossGroupsPosition
                           ?? throw new ApplicationFailureException(
                               "Across-groups qualification path is missing a position.",
                               ApplicationErrorCodes.QualificationCandidatesEmpty);

            if (derivedCache.TryGetValue(position, out var derived)) return derived;

            var standingRules = sourceStage.Regulation.StandingRules
                ?? throw new ApplicationFailureException(
                    "Standing rules are required for across-groups qualification.",
                    StandingErrorCodes.RulesRequired);
            derived = CrossGroupStandingAssembler.Build(
                sourceStage.Groups,
                groupStandings,
                position,
                matches,
                standingRules,
                CalculateStanding.ToStandingPenalties(sourceStage.Penalties));
            derivedCache[position] = derived;

            return derived;
        }

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

    private static Stage ResolveCanonicalStage(
        StageId stageId,
        IReadOnlyList<Stage> competitionStages)
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
