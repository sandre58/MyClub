// -----------------------------------------------------------------------
// <copyright file="GroupStageMatchday.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between GroupStage entities and Matchday entities.
/// This entity enables group stages to organize matches into structured rounds or matchdays
/// while maintaining proper Entity Framework Core relationship mapping.
/// </summary>
/// <param name="entity">The group stage that organizes the matchdays.</param>
/// <param name="matchday">The matchday that belongs to the group stage.</param>
/// <remarks>
/// The GroupStageMatchday join entity facilitates group-based tournament organization,
/// enabling tournament group phases to structure matches into organized rounds similar
/// to league-style scheduling within the broader tournament context.
/// </remarks>
internal sealed class GroupStageMatchday(GroupStage entity, Matchday matchday) : EntityLink<GroupStage, Matchday, StageId, MatchdayId>(entity, matchday, entity.Id, matchday.Id)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private GroupStageMatchday()
        : this(null!, null!) { }
}
