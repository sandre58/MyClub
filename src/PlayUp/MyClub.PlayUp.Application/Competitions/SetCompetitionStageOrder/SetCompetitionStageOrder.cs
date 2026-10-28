// -----------------------------------------------------------------------
// <copyright file="SetCompetitionStageOrder.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: reorder competition stages.
/// </summary>
/// <remarks>
/// Delegates the permutation to <see cref="Competition.SetStageOrder"/>.
/// Does not read or rewrite qualification or progression rules.
/// The caller commits with one SaveChanges.
/// </remarks>
public static class SetCompetitionStageOrder
{
    /// <summary>
    /// Sets <paramref name="competition"/> stage order to <paramref name="orderedStageIds"/>.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="orderedStageIds">Permutation of the current stage ids.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(
        Competition competition,
        IReadOnlyList<StageId> orderedStageIds,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(orderedStageIds);
        ArgumentNullException.ThrowIfNull(clock);

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Stage order cannot change while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        competition.SetStageOrder(orderedStageIds, clock);
    }
}
