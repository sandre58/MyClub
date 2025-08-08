// -----------------------------------------------------------------------
// <copyright file="GroupTeam.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between Group entities and team references.
/// This entity enables groups to contain multiple teams while supporting both concrete teams
/// and virtual team references for complex tournament scenarios.
/// </summary>
/// <param name="entityId">The identifier of the group that contains the teams.</param>
/// <param name="team">The team reference assigned to the group.</param>
/// <remarks>
/// The GroupTeam join entity facilitates team organization within tournament group structures,
/// enabling sophisticated group-based competition management with support for both known teams
/// and teams to be determined through qualification processes.
/// </remarks>
internal sealed class GroupTeam(GroupId entityId, TeamReference team) : EntityTeam<GroupId>(entityId, team)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private GroupTeam()
        : this(null!, null!)
    {
    }
}
