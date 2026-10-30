// -----------------------------------------------------------------------
// <copyright file="StructureHubNextStepResolver.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Selects at most one structural next step for the Structure hub (fail-closed).
/// </summary>
public static class StructureHubNextStepResolver
{
    private enum HubNextStepTier
    {
        Resume = 1,
        Prepare = 2,
        ApplyDraw = 3,
        RunDraw = 4,
        Materialize = 5,
        Start = 6,
        Complete = 7
    }

    /// <summary>
    /// Resolves the hub next step from an assembled Structure view and loaded stages.
    /// </summary>
    public static StructureHubNextStepDto? Decide(
        Competition competition,
        IReadOnlyList<Stage> competitionStages,
        StructureViewDto view,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(view);

        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            return null;
        }

        if (view.Stages.Count == 0)
        {
            return null;
        }

        var stageById = competitionStages.ToDictionary(stage => stage.Id.Value);
        var candidates = new List<(HubNextStepTier Tier, StructureHubNextStepDto Decision)>();

        foreach (var hub in view.Stages)
        {
            if (!stageById.TryGetValue(hub.StageId, out var stage))
            {
                continue;
            }

            IReadOnlyList<MatchSummaryRow>? summaries = null;
            matchSummariesByStage?.TryGetValue(stage.Id, out summaries);

            if (TryPerStageCandidate(competition, stage, competitionStages, hub, summaries, out var tier, out var decision))
            {
                candidates.Add((tier, decision));
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        var bestTier = candidates.Min(candidate => candidate.Tier);
        var atBest = candidates.Where(candidate => candidate.Tier == bestTier).ToArray();
        return atBest.Length == 1 ? atBest[0].Decision : null;
    }

    private static bool TryPerStageCandidate(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        StructureStageHubSummaryDto hub,
        IReadOnlyList<MatchSummaryRow>? summaries,
        out HubNextStepTier tier,
        out StructureHubNextStepDto decision)
    {
        if (TryResume(hub, out decision))
        {
            tier = HubNextStepTier.Resume;
            return true;
        }

        if (TryPrepare(hub, out decision))
        {
            tier = HubNextStepTier.Prepare;
            return true;
        }

        if (TryApplyDraw(stage, competitionStages, summaries, out decision))
        {
            tier = HubNextStepTier.ApplyDraw;
            return true;
        }

        if (TryRunDraw(hub, out decision))
        {
            tier = HubNextStepTier.RunDraw;
            return true;
        }

        if (TryMaterialize(hub, out decision))
        {
            tier = HubNextStepTier.Materialize;
            return true;
        }

        if (TryStart(hub, out decision))
        {
            tier = HubNextStepTier.Start;
            return true;
        }

        if (TryComplete(hub, out decision))
        {
            tier = HubNextStepTier.Complete;
            return true;
        }

        tier = default;
        decision = null!;
        return false;
    }

    private static bool TryResume(StructureStageHubSummaryDto hub, out StructureHubNextStepDto decision)
    {
        var revert = hub.Lifecycle?.RevertTransition;
        if (revert is { Code: OverviewAssembler.ActionResumeStage, Enabled: true })
        {
            decision = Pack(hub, OverviewAssembler.ActionResumeStage);
            return true;
        }

        decision = null!;
        return false;
    }

    private static bool TryPrepare(StructureStageHubSummaryDto hub, out StructureHubNextStepDto decision)
    {
        var transition = hub.Lifecycle?.Transition;
        if (transition is { Code: OverviewAssembler.ActionPrepareStage, Enabled: true })
        {
            decision = Pack(hub, OverviewAssembler.ActionPrepareStage);
            return true;
        }

        decision = null!;
        return false;
    }

    private static bool TryApplyDraw(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<MatchSummaryRow>? summaries,
        out StructureHubNextStepDto decision)
    {
        var snapshots = summaries is null
            ? []
            : MaterializedParticipantSnapshots.FromSummaryRows(summaries);
        var hasAttached = summaries is not null
            ? StageAttachedProduction.HasAttachedMatches(stage, summaries)
            : StructureSkeleton.CountAttachedMatches(stage) > 0;
        var context = new DrawActionContext(stage, competitionStages, snapshots, hasAttached);
        foreach (var draw in stage.Draws)
        {
            if (DrawActionEvaluator.Evaluate(context, draw).Apply.State != StageDrawActionState.Available)
            {
                continue;
            }

            decision = Pack(stage, OverviewAssembler.ActionApplyDraw);
            return true;
        }

        decision = null!;
        return false;
    }

    private static bool TryRunDraw(
        StructureStageHubSummaryDto hub,
        out StructureHubNextStepDto decision)
    {
        if (hub.DrawCreateLaunch.Allowed)
        {
            decision = Pack(hub, StructureViewAssembler.NextStepRunDraw);
            return true;
        }

        decision = null!;
        return false;
    }

    private static bool TryMaterialize(StructureStageHubSummaryDto hub, out StructureHubNextStepDto decision)
    {
        if (hub.Materialize is { CommandEnabled: true })
        {
            decision = Pack(hub, hub.Materialize.CommandCode);
            return true;
        }

        decision = null!;
        return false;
    }

    private static bool TryStart(StructureStageHubSummaryDto hub, out StructureHubNextStepDto decision)
    {
        var transition = hub.Lifecycle?.Transition;
        if (transition is { Code: OverviewAssembler.ActionStartStage, Enabled: true })
        {
            decision = Pack(hub, OverviewAssembler.ActionStartStage);
            return true;
        }

        decision = null!;
        return false;
    }

    private static bool TryComplete(StructureStageHubSummaryDto hub, out StructureHubNextStepDto decision)
    {
        var transition = hub.Lifecycle?.Transition;
        if (transition is { Code: OverviewAssembler.ActionCompleteStage, Enabled: true })
        {
            decision = Pack(hub, OverviewAssembler.ActionCompleteStage);
            return true;
        }

        decision = null!;
        return false;
    }

    private static StructureHubNextStepDto Pack(StructureStageHubSummaryDto hub, string code) =>
        new(code, hub.StageId, hub.Name);

    private static StructureHubNextStepDto Pack(Stage stage, string code) =>
        new(code, stage.Id.Value, stage.Name.Value);
}
