// -----------------------------------------------------------------------
// <copyright file="DrawActionEvaluator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Shared projection of draw lifecycle actions. Commands throw the same block code;
/// reads expose it. <see cref="StageDrawActionState.Available"/> does not guarantee success.
/// </summary>
internal static class DrawActionEvaluator
{
    internal static DrawActionContext ForCommand(Stage stage, IReadOnlyList<Match>? stageMatches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        var matches = stageMatches ?? [];
        return new DrawActionContext(
            stage,
            [stage],
            MaterializedParticipantSnapshots.FromMatches(matches),
            StageAttachedProduction.HasAttachedMatches(stage, matches));
    }

    internal static bool NeedsGroupDraftParticipantRows(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return stage.Status == StageStatus.Draft
            && stage.Groups.Count > 0
            && StructureSkeleton.CountAttachedMatches(stage) > 0
            && stage.Draws.Any(draw => draw.Kind == DrawResolutionKind.Group);
    }

    internal static DrawActionsAssessment Evaluate(DrawActionContext context, Draw draw)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(draw);
        return new DrawActionsAssessment(
            EvaluateCancel(context.Stage, draw),
            EvaluateRelease(context, draw, treatAsCancelled: false),
            EvaluateApply(context, draw),
            EvaluatePublishAndApply(context, draw),
            EvaluateRedo(context, draw));
    }

    /// <summary>
    /// Read projection. A closed competition hides actions; the executor still rejects the command.
    /// </summary>
    internal static DrawActionsAssessment EvaluateForRead(
        DrawActionContext context,
        Draw draw,
        CompetitionStatus competitionStatus)
    {
        var assessment = Evaluate(context, draw);
        return competitionStatus is CompetitionStatus.Completed or CompetitionStatus.Archived
            ? assessment.HideForClosedCompetition()
            : assessment;
    }

    internal static Guid? ReleaseCandidateDrawId(DrawActionContext context, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(stage);

        Guid? candidate = null;
        foreach (var draw in stage.Draws)
        {
            if (draw.Status != DrawStatus.Cancelled)
            {
                continue;
            }

            var release = EvaluateRelease(context, draw, treatAsCancelled: false);
            if (release.ReleasableCount >= 1)
            {
                candidate = draw.Id.Value;
            }
        }

        return candidate;
    }

    private static DrawActionAssessment EvaluateCancel(Stage stage, Draw draw)
    {
        if (draw.Status == DrawStatus.Cancelled)
        {
            return DrawActionAssessment.Hidden;
        }

        if (stage.Status is not (StageStatus.Draft or StageStatus.Ready))
        {
            return DrawActionAssessment.Block(StageErrorCodes.StructureLocked);
        }

        return draw.Status is DrawStatus.Draft or DrawStatus.Published
            ? DrawActionAssessment.Available
            : DrawActionAssessment.Block(StageErrorCodes.DrawInvalidTransition);
    }

    private static DrawReleaseAssessment EvaluateRelease(
        DrawActionContext context,
        Draw draw,
        bool treatAsCancelled)
    {
        var count = CountReleasable(context.Stage, draw);
        var block = TryReleaseBlock(context, draw, treatAsCancelled);
        if (block is not null)
        {
            return new DrawReleaseAssessment(StageDrawActionState.Blocked, block, count);
        }

        return count == 0
            ? new DrawReleaseAssessment(StageDrawActionState.Hidden, null, 0)
            : new DrawReleaseAssessment(StageDrawActionState.Available, null, count);
    }

    private static string? TryReleaseBlock(DrawActionContext context, Draw draw, bool treatAsCancelled)
    {
        if (draw.Kind is not (DrawResolutionKind.Slot or DrawResolutionKind.Group))
        {
            return ApplicationErrorCodes.DrawKindNotSupported;
        }

        if (!treatAsCancelled && draw.Status != DrawStatus.Cancelled)
        {
            return ApplicationErrorCodes.DrawReleaseRequiresCancelled;
        }

        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return ApplicationErrorCodes.DrawReleaseFailure;
        }

        if (PublishedAppliedBlocksRelease(context.Stage, draw, ignoreSelf: treatAsCancelled))
        {
            return ApplicationErrorCodes.DrawReleaseFailure;
        }

        if (context.Stage.Status is not (StageStatus.Draft or StageStatus.Ready))
        {
            return StageErrorCodes.StructureLocked;
        }

        if (draw.Kind == DrawResolutionKind.Group
            && context.Stage.Status == StageStatus.Ready
            && context.HasAttachedMatches)
        {
            return ApplicationErrorCodes.DrawReleaseBlockedByAttachedMatches;
        }

        return CoherenceBlock(context, draw);
    }

    private static DrawActionAssessment EvaluateApply(DrawActionContext context, Draw draw)
    {
        if (DrawAppliedState.IsApplied(draw, context.Stage))
        {
            return DrawActionAssessment.Hidden;
        }

        var block = TryApplyBlock(context, draw);
        return block is null ? DrawActionAssessment.Available : DrawActionAssessment.Block(block);
    }

    private static string? TryApplyBlock(DrawActionContext context, Draw draw)
    {
        if (draw.Kind is not (DrawResolutionKind.Slot or DrawResolutionKind.Group))
        {
            return ApplicationErrorCodes.DrawKindNotSupported;
        }

        if (draw.Status != DrawStatus.Published)
        {
            return ApplicationErrorCodes.DrawApplyFailure;
        }

        if (draw.Resolution.State != DrawResolutionState.Resolved
            || draw.Resolution.ResolvedKind != draw.Kind
            || draw.Inputs is null)
        {
            return ApplicationErrorCodes.DrawApplyFailure;
        }

        if (StructureOperationalExitEditGuard.DenialCode(context.Stage, context.HasAttachedMatches) is { } denial)
        {
            return denial;
        }

        if (PlacementBlock(context.Stage, draw) is { } placement)
        {
            return placement;
        }

        return DrawConsumptionConformance.IsObsoleteForConsumption(draw, context.Stage)
            ? ApplicationErrorCodes.DrawObsoleteForConsumption
            : null;
    }

    private static DrawActionAssessment EvaluatePublishAndApply(DrawActionContext context, Draw draw)
    {
        if (draw.Kind is not (DrawResolutionKind.Slot or DrawResolutionKind.Group)
            || draw.Status != DrawStatus.Draft
            || draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return DrawActionAssessment.Hidden;
        }

        if (draw.Inputs is null)
        {
            return DrawActionAssessment.Block(StageErrorCodes.DrawInvalidTransition);
        }

        if (StructureOperationalExitEditGuard.DenialCode(context.Stage, context.HasAttachedMatches) is { } denial)
        {
            return DrawActionAssessment.Block(denial);
        }

        if (PlacementBlock(context.Stage, draw) is { } placement)
        {
            return DrawActionAssessment.Block(placement);
        }

        return DrawConsumptionConformance.ResolutionEntriesDiverge(draw, context.Stage)
            ? DrawActionAssessment.Block(ApplicationErrorCodes.DrawObsoleteForConsumption)
            : DrawActionAssessment.Available;
    }

    private static DrawActionAssessment EvaluateRedo(DrawActionContext context, Draw draw)
    {
        if (!DrawAppliedState.IsApplied(draw, context.Stage))
        {
            return DrawActionAssessment.Hidden;
        }

        var release = EvaluateRelease(context, draw, treatAsCancelled: true);
        if (release.State == StageDrawActionState.Blocked)
        {
            return DrawActionAssessment.Block(release.BlockCode!);
        }

        if (context.Stage.DirectAssignments.Count > 0)
        {
            return DrawActionAssessment.Block(ApplicationErrorCodes.DrawLaunchDirectAssignmentPresent);
        }

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            context.Stage,
            context.CompetitionStages,
            draw.Kind,
            AlignedSlotKeys(context.Stage, draw),
            AlignedGroupEntryIds(context.Stage, draw));
        return facts.BlockingPlaceCount > 0
            ? DrawActionAssessment.Block(ApplicationErrorCodes.DrawLaunchGridNotVacant)
            : DrawActionAssessment.Available;
    }

    private static string? PlacementBlock(Stage stage, Draw draw)
    {
        var pool = draw.Inputs!.Entries;
        if (draw.Kind == DrawResolutionKind.Slot)
        {
            foreach (var placement in draw.Resolution.SlotResults)
            {
                var slot = stage.FindSlot(placement.SlotKey);
                if (slot is null || !pool.Contains(placement.EntryId) || IsDirectAssignment(stage, placement.SlotKey))
                {
                    return ApplicationErrorCodes.DrawApplyFailure;
                }

                if (slot.EntryId is { } current && !current.Equals(placement.EntryId))
                {
                    return ApplicationErrorCodes.DrawApplyFailure;
                }
            }

            return null;
        }

        if (draw.Kind != DrawResolutionKind.Group)
        {
            return null;
        }

        foreach (var placement in draw.Resolution.GroupResults)
        {
            if (stage.FindGroup(placement.GroupId) is null || !pool.Contains(placement.EntryId))
            {
                return ApplicationErrorCodes.DrawApplyFailure;
            }
        }

        return null;
    }

    private static string? CoherenceBlock(DrawActionContext context, Draw draw)
    {
        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return null;
        }

        try
        {
            if (draw.Kind == DrawResolutionKind.Slot)
            {
                var proposed = MaterializedOpponentCoherence.CopySlotOccupants(context.Stage);
                var changed = false;
                foreach (var placement in draw.Resolution.SlotResults)
                {
                    if (IsDirectAssignment(context.Stage, placement.SlotKey) || !SlotAligned(context.Stage, placement))
                    {
                        continue;
                    }

                    var slot = context.Stage.FindSlot(placement.SlotKey);
                    if (slot is null)
                    {
                        continue;
                    }

                    proposed[slot.SlotKey] = null;
                    changed = true;
                }

                if (!changed)
                {
                    return null;
                }

                MaterializedOpponentCoherence.EnsureCupPairsCoherent(
                    context.Stage,
                    proposed,
                    context.Snapshots);
                return null;
            }

            if (draw.Kind != DrawResolutionKind.Group)
            {
                return null;
            }

            foreach (var placement in draw.Resolution.GroupResults)
            {
                if (context.Stage.FindGroup(placement.GroupId)?.EntryIds.Contains(placement.EntryId) != true)
                {
                    continue;
                }

                MaterializedOpponentCoherence.EnsureEntryNotBoundToAttachedMatch(
                    context.Stage,
                    placement.EntryId,
                    context.Snapshots);
            }

            return null;
        }
        catch (DomainException exception)
        {
            return exception.Code ?? StageErrorCodes.MaterializedOpponentConflict;
        }
    }

    private static int CountReleasable(Stage stage, Draw draw)
    {
        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return 0;
        }

        return draw.Kind switch
        {
            DrawResolutionKind.Slot => draw.Resolution.SlotResults.Count(placement =>
                !IsDirectAssignment(stage, placement.SlotKey) && SlotAligned(stage, placement)),
            DrawResolutionKind.Group => draw.Resolution.GroupResults.Count(placement =>
                stage.FindGroup(placement.GroupId)?.EntryIds.Contains(placement.EntryId) == true),
            _ => 0
        };
    }

    private static HashSet<string> AlignedSlotKeys(Stage stage, Draw draw)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (draw.Kind != DrawResolutionKind.Slot || draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return keys;
        }

        foreach (var placement in draw.Resolution.SlotResults)
        {
            if (IsDirectAssignment(stage, placement.SlotKey) || !SlotAligned(stage, placement))
            {
                continue;
            }

            var slot = stage.FindSlot(placement.SlotKey);
            if (slot is not null)
            {
                keys.Add(slot.SlotKey);
            }
        }

        return keys;
    }

    private static HashSet<EntryId> AlignedGroupEntryIds(Stage stage, Draw draw)
    {
        var entries = new HashSet<EntryId>();
        if (draw.Kind != DrawResolutionKind.Group || draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return entries;
        }

        foreach (var placement in draw.Resolution.GroupResults)
        {
            if (stage.FindGroup(placement.GroupId)?.EntryIds.Contains(placement.EntryId) == true)
            {
                entries.Add(placement.EntryId);
            }
        }

        return entries;
    }

    private static bool PublishedAppliedBlocksRelease(Stage stage, Draw draw, bool ignoreSelf) =>
        stage.Draws.Any(other =>
            (!ignoreSelf || !other.Id.Equals(draw.Id))
            && other.Status == DrawStatus.Published
            && DrawAppliedState.IsApplied(other, stage));

    private static bool SlotAligned(Stage stage, SlotDrawPlacement placement)
    {
        var slot = stage.FindSlot(placement.SlotKey);
        return slot?.EntryId is { } current && current.Equals(placement.EntryId);
    }

    private static bool IsDirectAssignment(Stage stage, string slotKey) =>
        stage.DirectAssignments.Any(assignment =>
            string.Equals(assignment.SlotKey, slotKey, StringComparison.Ordinal));
}

/// <summary>Inputs the evaluator needs beyond the draw itself.</summary>
internal sealed class DrawActionContext(
    Stage stage,
    IReadOnlyList<Stage> competitionStages,
    IReadOnlyList<MatchParticipantSnapshot> snapshots,
    bool hasAttachedMatches)
{
    internal Stage Stage { get; } = stage;

    internal IReadOnlyList<Stage> CompetitionStages { get; } = competitionStages;

    internal IReadOnlyList<MatchParticipantSnapshot> Snapshots { get; } = snapshots;

    internal bool HasAttachedMatches { get; } = hasAttachedMatches;
}

internal sealed record DrawActionAssessment(StageDrawActionState State, string? BlockCode)
{
    internal static DrawActionAssessment Hidden { get; } = new(StageDrawActionState.Hidden, null);

    internal static DrawActionAssessment Available { get; } = new(StageDrawActionState.Available, null);

    internal static DrawActionAssessment Block(string code) => new(StageDrawActionState.Blocked, code);
}

internal sealed record DrawReleaseAssessment(
    StageDrawActionState State,
    string? BlockCode,
    int ReleasableCount);

internal sealed record DrawActionsAssessment(
    DrawActionAssessment Cancel,
    DrawReleaseAssessment Release,
    DrawActionAssessment Apply,
    DrawActionAssessment PublishAndApply,
    DrawActionAssessment Redo)
{
    internal DrawActionsAssessment HideForClosedCompetition() =>
        new(
            DrawActionAssessment.Hidden,
            Release with { State = StageDrawActionState.Hidden, BlockCode = null },
            DrawActionAssessment.Hidden,
            DrawActionAssessment.Hidden,
            DrawActionAssessment.Hidden);
}

internal static class DrawActionFailure
{
    internal static void Throw(string code, string message)
    {
        if (code.StartsWith("Stage.", StringComparison.Ordinal))
        {
            throw new DomainException(message, code);
        }

        throw new ApplicationFailureException(message, code);
    }
}
