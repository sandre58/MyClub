// -----------------------------------------------------------------------
// <copyright file="LeagueMatchdayConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for LeagueMatchday join entities, establishing
/// the many-to-many relationship mapping between League entities and Matchday entities
/// for round-robin league organization and structured season scheduling.
/// </summary>
/// <remarks>
/// The LeagueMatchdayConfiguration provides standardized join entity configuration for
/// leagues containing organized matchdays, enabling traditional league formats with
/// structured round organization where all teams play each other over organized
/// matchdays throughout the competition season in round-robin arrangements.
/// </remarks>
internal sealed class LeagueMatchdayConfiguration : IEntityTypeConfiguration<LeagueMatchday>
{
    /// <summary>
    /// Configures the LeagueMatchday join entity using the standardized join entity
    /// configuration pattern for League-Matchday relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for LeagueMatchday configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join entity pattern establishing composite
    /// primary keys, foreign key relationships with appropriate cascade behaviors, and
    /// index optimization for efficient matchday retrieval by league while supporting
    /// structured season organization for traditional round-robin league competitions.
    /// </remarks>
    public void Configure(EntityTypeBuilder<LeagueMatchday> builder) => builder.ConfigureJoinEntity<LeagueMatchday, League, Matchday, CompetitionId, MatchdayId>();
}
