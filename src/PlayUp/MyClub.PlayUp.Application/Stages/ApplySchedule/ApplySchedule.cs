// -----------------------------------------------------------------------
// <copyright file="ApplySchedule.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Scheduling;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: materialize a successful <see cref="SchedulingResult"/> onto Stage match placements.
/// Caller supplies Targets explicitly; does not open Stage collections for direct mutation.
/// </summary>
/// <remarks>
/// V1 Application: Domain batch Apply only. No unit of work / EF transaction here.
/// </remarks>
public static class ApplySchedule
{
    /// <summary>
    /// Applies a <see cref="SchedulingResult"/> Success onto an already-loaded Stage.
    /// </summary>
    /// <param name="stage">Stage that owns match attachments and placements.</param>
    /// <param name="result">Generation outcome; must be Success.</param>
    /// <param name="targets">Match identities authorized for write/replace.</param>
    /// <exception cref="ApplicationFailureException">Thrown when <paramref name="result"/> is not Success.</exception>
    public static void Execute(
        Stage stage,
        SchedulingResult result,
        IReadOnlyList<MatchId> targets)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(targets);

        if (!result.IsSuccess)
        {
            throw new ApplicationFailureException(
                "ApplySchedule requires a Success scheduling result.",
                ApplicationErrorCodes.ScheduleApplyFailure);
        }

        var placements = result.Schedule!.Assignments
            .Select(a => new MatchPlacement(a.MatchId, a.Start, a.ResourceId))
            .ToArray();

        stage.ApplyMatchPlacements(placements, targets);
    }
}
