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
/// Application use case: configuration gates then WhoFeeds then local <see cref="StageAggregate.Prepare"/>.
/// </summary>
/// <remarks>
/// Domain validates local structure and local path destinations.
/// Application validates TieFormat when Progression references a Round fixture,
/// outbound StageId/SlotKey destinations, and WhoFeeds (inbound).
/// </remarks>
public static class PrepareStage
{
    /// <summary>
    /// Validates configuration gates and global slot feeds when needed, then prepares the stage.
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

        EnsureStageInCompetition(target, competitionStages);
        EnsureTieFormatForProgressionSources(target);
        EnsureOutboundPathDestinations(target, competitionStages);

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

    private static void EnsureStageInCompetition(
        StageAggregate target,
        IReadOnlyList<StageAggregate> competitionStages)
    {
        if (competitionStages.Any(s => s.Id.Equals(target.Id)))
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Stage '{target.Id}' is not part of the competition stages list.",
            ApplicationErrorCodes.StageNotInCompetition);
    }

    /// <summary>
    /// D10-A / D10-B: a Round needs TieFormat when Progression references one of its fixtures.
    /// </summary>
    private static void EnsureTieFormatForProgressionSources(StageAggregate target)
    {
        if (target.Regulation.ProgressionRules is not { } progression)
        {
            return;
        }

        var referencedFixtureIds = progression.Paths
            .Select(p => p.SourceFixtureId)
            .ToHashSet();

        foreach (var round in target.Rounds)
        {
            if (round.TieFormat is not null)
            {
                continue;
            }

            if (!round.Fixtures.Any(f => referencedFixtureIds.Contains(f.Id)))
            {
                continue;
            }

            throw new ApplicationFailureException(
                $"Round '{round.Name}' requires a TieFormat because one of its fixtures is referenced by a progression rule.",
                ApplicationErrorCodes.TieFormatRequired);
        }
    }

    /// <summary>
    /// D10-D: outbound Qualification / Progression destinations must exist in the competition
    /// and expose the declared SlotKey.
    /// </summary>
    private static void EnsureOutboundPathDestinations(
        StageAggregate source,
        IReadOnlyList<StageAggregate> competitionStages)
    {
        if (source.Regulation.QualificationRules is { } qualification)
        {
            foreach (var path in qualification.Paths)
            {
                EnsureOutboundDestination(
                    source,
                    competitionStages,
                    path.Destination.StageId,
                    path.Destination.SlotKey,
                    "Qualification");
            }
        }

        if (source.Regulation.ProgressionRules is not { } progression)
        {
            return;
        }

        foreach (var path in progression.Paths)
        {
            EnsureOutboundDestination(
                source,
                competitionStages,
                path.Destination.StageId,
                path.Destination.SlotKey,
                "Progression");
        }
    }

    private static void EnsureOutboundDestination(
        StageAggregate source,
        IReadOnlyList<StageAggregate> competitionStages,
        StageId destinationStageId,
        string slotKey,
        string mechanism)
    {
        if (destinationStageId.Equals(source.Id))
        {
            return;
        }

        var destination = competitionStages.FirstOrDefault(s => s.Id.Equals(destinationStageId))
            ?? throw new ApplicationFailureException(
                $"{mechanism} destination stage '{destinationStageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);

        if (destination.FindSlot(slotKey) is not null)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"{mechanism} destination slot '{slotKey}' was not found on stage '{destination.Id}'.",
            ApplicationErrorCodes.DanglingFeedTarget);
    }
}
