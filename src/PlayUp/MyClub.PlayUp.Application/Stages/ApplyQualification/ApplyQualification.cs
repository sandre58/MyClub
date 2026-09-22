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
/// Application use case: apply qualification paths from standings onto destination population
/// and optionally Place (Auto dual-write).
/// </summary>
/// <remarks>
/// Prefights destination stages before any mutation.
/// Replace local only — no cascade. One path → one entry → population;
/// when destination has SlotKey, also ApplyResolvedEntry on that slot.
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
    public static IReadOnlyList<QualificationInstruction> Execute(
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
    public static IReadOnlyList<QualificationInstruction> Execute(
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
    public static IReadOnlyList<QualificationInstruction> Execute(
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
        var outcomes = new (QualificationPath Path, QualificationInstruction? Instruction)[paths.Length];
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

        var applied = new List<QualificationInstruction>(outcomes.Length);
        foreach (var (path, instruction) in outcomes)
        {
            var destination = ResolveCanonicalStage(path.Destination.StageId, competitionStages);
            if (destination.Id.Equals(canonicalSource.Id))
            {
                throw new ApplicationFailureException(
                    "Qualification destination cannot target the source stage.",
                    ApplicationErrorCodes.DanglingFeedTarget);
            }

            if (instruction is null)
            {
                continue;
            }

            if (path.Destination.TargetsPopulation || path.Destination.TargetsForm)
            {
                continue;
            }

            if (path.Destination.TargetsGroup)
            {
                if (destination.FindGroup(path.Destination.GroupId!.Value) is null)
                {
                    throw new ApplicationFailureException(
                        $"Qualification destination group '{path.Destination.GroupId}' was not found on stage '{destination.Id}'.",
                        ApplicationErrorCodes.DanglingFeedTarget);
                }

                continue;
            }

            if (destination.FindSlot(path.Destination.SlotKey!) is null)
            {
                throw new ApplicationFailureException(
                    $"Qualification destination slot '{path.Destination.SlotKey}' was not found on stage '{destination.Id}'.",
                    ApplicationErrorCodes.DanglingFeedTarget);
            }

            SlotOccupancyConflictGuard.EnsureCompatible(
                destination,
                new SlotAssignmentInstruction(
                    destination.Id,
                    path.Destination.SlotKey!,
                    instruction.EntryId));
        }

        foreach (var (path, instruction) in outcomes)
        {
            if (instruction is null)
            {
                continue;
            }

            var destination = ResolveCanonicalStage(path.Destination.StageId, competitionStages);

            // Population + Form: AddResolvedPopulationEntry materializes Composition once.
            // Form retains Placement intent on Path for WhoFeeds — no second Composition write.
            destination.AddResolvedPopulationEntry(instruction.EntryId, clock);
            if (path.Destination.TargetsGroup)
            {
                destination.ApplyResolvedGroupEntry(
                    path.Destination.GroupId!.Value,
                    instruction.EntryId);
            }
            else if (path.Destination.TargetsSlot)
            {
                destination.ApplyResolvedEntry(path.Destination.SlotKey!, instruction.EntryId, clock);
            }

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
