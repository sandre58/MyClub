// -----------------------------------------------------------------------
// <copyright file="SlotFeedSnapshotAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stage;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Builds a <see cref="SlotFeedSnapshot"/> from already-loaded stages (no repository required).
/// </summary>
public static class SlotFeedSnapshotAssembler
{
    /// <summary>
    /// Assembles inbound and local configuration feeds for <paramref name="target"/>.
    /// </summary>
    /// <param name="target">Destination stage whose slots are resolved.</param>
    /// <param name="competitionStages">All stages of the competition (already loaded).</param>
    /// <returns>An immutable domain snapshot.</returns>
    public static SlotFeedSnapshot Assemble(
        StageAggregate target,
        IReadOnlyList<StageAggregate> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(competitionStages);

        if (competitionStages.All(s => !s.Id.Equals(target.Id)))
        {
            throw new ApplicationFailureException(
                $"Stage '{target.Id}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        var slotKeys = target.Slots.Select(s => s.SlotKey).ToArray();

        var directs = target.DirectAssignments
            .Select(a => new DirectFeedSource(a.SlotKey, a.EntryId))
            .ToArray();

        var qualifications = new List<QualificationFeedSource>();
        var progressions = new List<ProgressionFeedSource>();

        foreach (var stage in competitionStages)
        {
            if (stage.Regulation.QualificationRules is { } qualificationRules)
            {
                foreach (var path in qualificationRules.Paths)
                {
                    if (!path.Destination.StageId.Equals(target.Id))
                    {
                        continue;
                    }

                    EnsureSlotExists(target, path.Destination.SlotKey, stage.Id, "Qualification");
                    qualifications.Add(
                        new QualificationFeedSource(stage.Id, path.Order, path.Destination.SlotKey));
                }
            }

            if (stage.Regulation.ProgressionRules is not { } progressionRules) continue;

            foreach (var path in progressionRules.Paths)
            {
                if (!path.Destination.StageId.Equals(target.Id))
                {
                    continue;
                }

                EnsureSlotExists(target, path.Destination.SlotKey, stage.Id, "Progression");
                progressions.Add(
                    new ProgressionFeedSource(
                        stage.Id,
                        path.SourceFixtureId,
                        path.Outcome,
                        path.Destination.SlotKey));
            }
        }

        return new SlotFeedSnapshot(
            target.Id,
            slotKeys,
            directs,
            qualifications,
            progressions,
            drawTargets: []);
    }

    private static void EnsureSlotExists(
        StageAggregate target,
        string destinationSlotKey,
        Domain.Common.StageId sourceStageId,
        string mechanism)
    {
        if (target.FindSlot(destinationSlotKey) is not null)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"{mechanism} path from stage '{sourceStageId}' targets unknown slot '{destinationSlotKey}'.",
            ApplicationErrorCodes.DanglingFeedTarget);
    }
}
