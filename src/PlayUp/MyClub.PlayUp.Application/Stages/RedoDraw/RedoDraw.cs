// -----------------------------------------------------------------------
// <copyright file="RedoDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: redo an applied draw in one transaction — cancel, release the aligned
/// placements, then preview a new resolution (create → configure Rerun inputs → generate).
/// </summary>
/// <remarks>
/// Reuses <see cref="CancelDraw"/>, <see cref="ReleaseDrawAlignedPlacements"/>, <see cref="CreateDraw"/>,
/// <see cref="ConfigureDrawInputs"/>, and <see cref="GenerateDrawResolution"/> against the same
/// already-loaded Stage. Mirrors the former client orchestration (Cancel → Release → Preview);
/// the caller saves once, so a failure at any step — including one the upfront <see cref="DrawActionEvaluator"/>
/// projection does not see (e.g. a sibling-stage feed claiming a released slot) — leaves nothing persisted.
/// </remarks>
public static class RedoDraw
{
    /// <summary>
    /// Redoes <paramref name="drawId"/> on <paramref name="stage"/>.
    /// </summary>
    /// <param name="stage">Stage that owns the draw.</param>
    /// <param name="drawId">Applied draw to redo.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <param name="competitionStages">All competition stages (Rerun launch-vacancy guard, feed resolution).</param>
    /// <param name="stageMatches">Matches attached on the stage (opponent coherence); empty when none.</param>
    /// <returns>The new draw's identity and generation outcome.</returns>
    public static RedoDrawResult Execute(
        Stage stage,
        DrawId drawId,
        IClock clock,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Match>? stageMatches = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var draw = stage.GetDraw(drawId);
        var redo = DrawActionEvaluator.Evaluate(
            DrawActionEvaluator.ForCommand(stage, stageMatches),
            draw).Redo;
        if (redo.State == StageDrawActionState.Blocked)
        {
            DrawActionFailure.Throw(
                redo.BlockCode!,
                $"Cannot redo draw '{draw.Id}' ({redo.BlockCode}).");
        }

        var kind = draw.Kind;

        CancelDraw.Execute(stage, drawId, clock);
        ReleaseDrawAlignedPlacements.Execute(stage, drawId, clock, stageMatches);

        var created = CreateDraw.Execute(stage, kind, clock, stageMatches);
        var inputs = DrawInputsFactory.CreateDefault(stage, kind, DrawInputsIntent.Rerun);
        ConfigureDrawInputs.Execute(
            stage,
            created.Id,
            inputs,
            DrawInputsIntent.Rerun,
            competitionStages,
            stageMatches);

        IReadOnlyList<string>? slotTargets = kind == DrawResolutionKind.Slot
            ? stage.Slots.Select(slot => slot.SlotKey).ToArray()
            : null;
        IReadOnlyList<GroupId>? groupTargets = kind == DrawResolutionKind.Group
            ? stage.Groups.Select(group => group.Id).ToArray()
            : null;

        var generation = GenerateDrawResolution.Execute(
            stage,
            created.Id,
            clock,
            slotTargets,
            groupTargets,
            stageMatches: stageMatches);

        return new RedoDrawResult(created.Id, generation.IsResolved, generation.IsNoSolution);
    }
}

/// <summary>
/// Outcome of <see cref="RedoDraw.Execute"/>: the newly created draw and its generation result.
/// </summary>
/// <param name="DrawId">Identity of the new draw (not <c>drawId</c> passed to <see cref="RedoDraw.Execute"/>).</param>
/// <param name="IsResolved">Whether generation produced a resolution.</param>
/// <param name="IsNoSolution">Whether generation recorded NoSolution.</param>
public sealed record RedoDrawResult(DrawId DrawId, bool IsResolved, bool IsNoSolution);
