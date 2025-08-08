// -----------------------------------------------------------------------
// <copyright file="GroupConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for Group entities, establishing comprehensive
/// mapping for group data including display properties, team assignments, and property
/// delegation patterns for tournament group management within group stages.
/// </summary>
/// <remarks>
/// The GroupConfiguration provides complete persistence mapping for tournament groups,
/// handling display information, team assignment relationships, and property delegation
/// to parent group stages for comprehensive group management within tournament structures
/// where groups organize teams for round-robin competition phases.
/// </remarks>
internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    /// <summary>
    /// Configures the Group entity with complete property mapping, team relationships,
    /// and property delegation patterns for group-based tournament organization.
    /// </summary>
    /// <param name="builder">The entity type builder for Group configuration.</param>
    /// <remarks>
    /// This configuration establishes display name ownership for group identification,
    /// auditable properties for group lifecycle tracking, team relationships through
    /// join entities for group member management, and property ignoring for delegated
    /// properties that are managed by the parent group stage entity.
    /// </remarks>
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ConfigureEntity<Group, GroupId>();

        builder.OwnsDisplayName(static x => x.DisplayName);
        builder.OwnsAuditableProperties();

        builder.HasManyTeams<Group, GroupId, GroupTeam>();

        builder.Ignore(static x => x.Matchdays);
        builder.Ignore(static x => x.StandingRules);
        builder.Ignore(static x => x.Labels);
    }
}
