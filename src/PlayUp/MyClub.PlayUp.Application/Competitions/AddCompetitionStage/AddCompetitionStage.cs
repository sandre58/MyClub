// -----------------------------------------------------------------------
// <copyright file="AddCompetitionStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: create and attach an additional Stage to a Competition (thin authoring).
/// </summary>
public static class AddCompetitionStage
{
    /// <summary>
    /// Creates a Draft stage and attaches it to the competition.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="stageName">Display name.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>The new stage (caller must persist).</returns>
    public static Stage Execute(Competition competition, string stageName, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Stages cannot be added while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }

        var stage = Stage.Create(
            competition.Id,
            new StageName(stageName),
            competition.Regulation,
            clock);
        competition.AddStage(stage.Id, clock);
        return stage;
    }
}
