// -----------------------------------------------------------------------
// <copyright file="GroupTeamConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for GroupTeam join entities, establishing
/// the many-to-many relationship mapping between Group entities and team references
/// for group-based tournament organization and team assignment.
/// </summary>
/// <remarks>
/// The GroupTeamConfiguration provides standardized join entity configuration for
/// groups containing team assignments, enabling group-based tournament phases where
/// teams are organized into groups for round-robin play within larger tournament
/// structures while supporting both concrete and virtual team references.
/// </remarks>
internal sealed class GroupTeamConfiguration : IEntityTypeConfiguration<GroupTeam>
{
    /// <summary>
    /// Configures the GroupTeam join entity using the standardized join team
    /// configuration pattern for Group-Team relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for GroupTeam configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join team pattern establishing composite
    /// primary keys, foreign key relationships, team reference conversion, and index
    /// optimization for efficient team retrieval by group while supporting polymorphic
    /// team references for complex tournament bracket scenarios.
    /// </remarks>
    public void Configure(EntityTypeBuilder<GroupTeam> builder) => builder.ConfigureJoinTeam<GroupTeam, Group, GroupId>();
}
