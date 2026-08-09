// -----------------------------------------------------------------------
// <copyright file="PrepareStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stage;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Application use case: WhoFeeds validation then local <see cref="StageAggregate.Prepare"/>.
/// </summary>
public static class PrepareStage
{
    /// <summary>
    /// Validates global slot feeds when needed, then prepares the stage.
    /// </summary>
    /// <param name="target">Stage to prepare.</param>
    /// <param name="competitionStages">All competition stages (already loaded).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Per-slot resolutions when feeds were validated; empty when the stage has no slots.</returns>
    public static IReadOnlyList<SlotFeedResolution> Execute(
        StageAggregate target,
        IReadOnlyList<StageAggregate> competitionStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);

        if (target.Slots.Count == 0)
        {
            target.Prepare(clock);
            return [];
        }

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, competitionStages);
        var resolutions = SlotFeedResolver.ResolveAll(snapshot);

        var failures = resolutions
            .Where(r => r.Status is not FeedResolutionStatus.Unique)
            .ToArray();
        if (failures.Length > 0)
        {
            var summary = string.Join(
                ", ",
                failures.Select(f => $"{f.SlotKey}:{f.Status}"));
            throw new ApplicationFailureException(
                $"Stage '{target.Id}' slot feeds are invalid: {summary}.",
                ApplicationErrorCodes.SlotFeedsInvalid);
        }

        target.Prepare(clock);
        return resolutions;
    }
}
