// -----------------------------------------------------------------------
// <copyright file="PrepareStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: configuration gates then WhoFeeds then local <see cref="Stage.Prepare"/>.
/// </summary>
/// <remarks>
/// Hosts must call this use case for Draft → Ready — not <see cref="Stage.Prepare"/> alone.
/// Domain validates local structure and local path destinations only.
/// Application validates outbound StageId/SlotKey destinations and WhoFeeds (inbound).
/// Null <c>Round.TieFormat</c> is allowed (effective OneLeg) — same contract as ApplyDraw / materialize.
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
        Stage target,
        IReadOnlyList<Stage> competitionStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(clock);

        EnsureStageInCompetition(target, competitionStages);
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
        Stage target,
        IReadOnlyList<Stage> competitionStages)
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
    /// D10-D: outbound Qualification / Progression destinations must exist in the competition
    /// and expose the declared SlotKey.
    /// </summary>
    private static void EnsureOutboundPathDestinations(
        Stage source,
        IReadOnlyList<Stage> competitionStages)
    {
        if (source.Regulation.QualificationRules is { } qualification)
        {
            foreach (var path in qualification.Paths)
            {
                if (path.Destination.TargetsPopulation)
                {
                    EnsureOutboundPopulationDestination(
                        source,
                        competitionStages,
                        path.Destination.StageId,
                        "Qualification");
                    continue;
                }

                if (path.Destination.TargetsForm)
                {
                    EnsureOutboundFormDestination(
                        source,
                        competitionStages,
                        path.Destination.StageId,
                        "Qualification");
                    continue;
                }

                if (path.Destination.TargetsGroup)
                {
                    EnsureOutboundGroupDestination(
                        source,
                        competitionStages,
                        path.Destination.StageId,
                        path.Destination.GroupId!.Value,
                        "Qualification");
                    continue;
                }

                EnsureOutboundDestination(
                    source,
                    competitionStages,
                    path.Destination.StageId,
                    path.Destination.SlotKey!,
                    "Qualification");
            }
        }

        if (source.Regulation.ProgressionRules is not { } progression)
        {
            return;
        }

        foreach (var path in progression.Paths)
        {
            if (path.Destination.TargetsPopulation)
            {
                EnsureOutboundPopulationDestination(
                    source,
                    competitionStages,
                    path.Destination.StageId,
                    "Progression");
                continue;
            }

            if (path.Destination.TargetsForm)
            {
                EnsureOutboundFormDestination(
                    source,
                    competitionStages,
                    path.Destination.StageId,
                    "Progression");
                continue;
            }

            if (path.Destination.TargetsGroup)
            {
                EnsureOutboundGroupDestination(
                    source,
                    competitionStages,
                    path.Destination.StageId,
                    path.Destination.GroupId!.Value,
                    "Progression");
                continue;
            }

            EnsureOutboundDestination(
                source,
                competitionStages,
                path.Destination.StageId,
                path.Destination.SlotKey!,
                "Progression");
        }
    }

    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "False positive")]
    private static void EnsureOutboundPopulationDestination(
        Stage source,
        IReadOnlyList<Stage> competitionStages,
        StageId destinationStageId,
        string mechanism)
    {
        if (destinationStageId.Equals(source.Id))
        {
            throw new ApplicationFailureException(
                $"{mechanism} population destination cannot target the source stage (use a slot destination for intra-phase placement).",
                ApplicationErrorCodes.DanglingFeedTarget);
        }

        if (competitionStages.Any(s => s.Id.Equals(destinationStageId)))
        {
            return;
        }

        throw new ApplicationFailureException(
            $"{mechanism} destination stage '{destinationStageId}' is not part of the competition stages list.",
            ApplicationErrorCodes.StageNotInCompetition);
    }

    /// <summary>
    /// ForForm is allowed only toward Championship / Swiss-shaped stages (authoring matrix).
    /// Not a Domain invariant — Prepare/API gate only.
    /// </summary>
    private static void EnsureOutboundFormDestination(
        Stage source,
        IReadOnlyList<Stage> competitionStages,
        StageId destinationStageId,
        string mechanism)
    {
        if (destinationStageId.Equals(source.Id))
        {
            throw new ApplicationFailureException(
                $"{mechanism} form destination cannot target the source stage.",
                ApplicationErrorCodes.DanglingFeedTarget);
        }

        var destination = competitionStages.FirstOrDefault(s => s.Id.Equals(destinationStageId))
            ?? throw new ApplicationFailureException(
                $"{mechanism} destination stage '{destinationStageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);

        if (IsFormPlacementEligible(destination))
        {
            return;
        }

        throw new ApplicationFailureException(
            $"{mechanism} form destination requires a Championship or Swiss stage structure.",
            ApplicationErrorCodes.DanglingFeedTarget);
    }

    private static bool IsFormPlacementEligible(Stage destination) =>
        destination.IsSwiss
        || (destination.Matchdays.Count > 0 && destination.Groups.Count == 0);

    private static void EnsureOutboundDestination(
        Stage source,
        IReadOnlyList<Stage> competitionStages,
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

    private static void EnsureOutboundGroupDestination(
        Stage source,
        IReadOnlyList<Stage> competitionStages,
        StageId destinationStageId,
        GroupId groupId,
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

        if (destination.FindGroup(groupId) is not null)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"{mechanism} destination group '{groupId}' was not found on stage '{destination.Id}'.",
            ApplicationErrorCodes.DanglingFeedTarget);
    }
}
