// -----------------------------------------------------------------------
// <copyright file="InsertCompetitionStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: atomic positioned birth of a competition stage.
/// </summary>
/// <remarks>
/// Validates the anchor, births via <see cref="AddCompetitionStage"/> (identity + skeleton, appended),
/// then places the new id with <see cref="Competition.SetStageOrder"/>.
/// The caller persists the new stage and the competition in one SaveChanges.
/// An invalid anchor or a closed lifecycle fails before birth, so nothing is attached.
/// </remarks>
public static class InsertCompetitionStage
{
    /// <summary>
    /// Creates a Draft stage and places it before or after <paramref name="anchorStageId"/>.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="intent">Validated format intent (name + kind + skeleton params).</param>
    /// <param name="anchorStageId">Existing stage the new stage is positioned against.</param>
    /// <param name="position">Before or after the anchor.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>The new structured stage (caller must persist in one SaveChanges).</returns>
    public static Stage Execute(
        Competition competition,
        StructureIntent intent,
        StageId anchorStageId,
        StageInsertPosition position,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable(competition);

        if (!Enum.IsDefined(position))
        {
            throw new ApplicationFailureException(
                $"Unknown stage insert position '{position}'. Expected Before or After.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        if (!competition.HasStage(anchorStageId))
        {
            throw new ApplicationFailureException(
                $"Anchor stage '{anchorStageId}' is not part of competition '{competition.Id}'.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        var existing = competition.StageIds.ToArray();
        var stage = AddCompetitionStage.Execute(competition, intent, clock);
        competition.SetStageOrder(Place(existing, stage.Id, anchorStageId, position), clock);
        return stage;
    }

    private static void EnsureStructureMutable(Competition competition)
    {
        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Stages cannot be inserted while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }

    private static List<StageId> Place(
        StageId[] existing,
        StageId newStageId,
        StageId anchorStageId,
        StageInsertPosition position)
    {
        var placed = new List<StageId>(existing.Length + 1);
        foreach (var id in existing)
        {
            if (position == StageInsertPosition.Before && id.Equals(anchorStageId))
            {
                placed.Add(newStageId);
            }

            placed.Add(id);

            if (position == StageInsertPosition.After && id.Equals(anchorStageId))
            {
                placed.Add(newStageId);
            }
        }

        return placed;
    }
}
