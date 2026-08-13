// -----------------------------------------------------------------------
// <copyright file="GenerateSchedule.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Scheduling;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: build a <see cref="SchedulingRequest"/> from Stage placements + Host inputs
/// and call <see cref="ScheduleGenerator.Generate"/>. Does not mutate Stage, Match, or placements.
/// </summary>
public static class GenerateSchedule
{
    /// <summary>
    /// Generates a schedule proposal for the given targets.
    /// </summary>
    /// <param name="stage">Stage owning attachments and current placements (Existing).</param>
    /// <param name="targetMatchIds">Match identities to schedule (exploration order).</param>
    /// <param name="horizon">Scheduling horizon.</param>
    /// <param name="timeGranularity">Start grid step.</param>
    /// <param name="timeZoneId">IANA time-zone id (not used for the grid).</param>
    /// <param name="matches">Match scheduling contexts (must cover Targets and Existing).</param>
    /// <param name="resources">Resource scheduling contexts.</param>
    /// <param name="minimumGapBetweenMatches">Optional same-resource gap.</param>
    /// <param name="precedences">Optional precedence constraints.</param>
    /// <param name="sameStarts">Optional same-start constraints.</param>
    /// <param name="minimumSeparations">Optional minimum-separation constraints.</param>
    /// <returns>Domain <see cref="SchedulingResult"/> (Success / NoSolution / InvalidRequest).</returns>
    public static SchedulingResult Execute(
        Stage stage,
        IReadOnlyList<MatchId> targetMatchIds,
        Horizon horizon,
        TimeGranularity timeGranularity,
        string timeZoneId,
        IReadOnlyList<MatchSchedulingContext> matches,
        IReadOnlyList<ResourceSchedulingContext> resources,
        MinimumGapBetweenMatches? minimumGapBetweenMatches = null,
        IReadOnlyList<Precedence>? precedences = null,
        IReadOnlyList<SameStart>? sameStarts = null,
        IReadOnlyList<MinimumSeparation>? minimumSeparations = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(targetMatchIds);
        ArgumentNullException.ThrowIfNull(horizon);
        ArgumentNullException.ThrowIfNull(timeGranularity);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(resources);

        foreach (var targetId in targetMatchIds)
        {
            if (!stage.HasMatch(targetId))
            {
                throw new ApplicationFailureException(
                    $"Target match '{targetId}' is not attached to stage '{stage.Id}'.",
                    ApplicationErrorCodes.ScheduleGenerationFailure);
            }
        }

        var existing = new Schedule(
        [
            .. stage.MatchPlacements.Select(p => new ScheduleAssignment(p.MatchId, p.ResourceId, p.Start))
        ]);

        var request = new SchedulingRequest(
            horizon,
            timeGranularity,
            timeZoneId,
            matches,
            resources,
            existing,
            targetMatchIds,
            minimumGapBetweenMatches,
            precedences,
            sameStarts,
            minimumSeparations);

        return ScheduleGenerator.Generate(request);
    }
}
