// -----------------------------------------------------------------------
// <copyright file="TournamentConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for Tournament entities, establishing the
/// many-to-many relationship with Stage entities for multiphase tournament organization.
/// This configuration enables complex tournament structures combining multiple stages.
/// </summary>
/// <remarks>
/// The TournamentConfiguration extends the base Competition configuration with
/// tournament-specific stage relationship management, enabling sophisticated
/// tournament formats that combine group phases, knockout rounds, and championship
/// stages within unified competition structures for comprehensive tournament administration.
/// </remarks>
internal sealed class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    /// <summary>
    /// Configures the Tournament entity with stage relationship management using
    /// the standardized many-to-many join entity pattern for multi-stage tournaments.
    /// </summary>
    /// <param name="builder">The entity type builder for Tournament configuration.</param>
    /// <remarks>
    /// This configuration establishes the Tournament-Stage many-to-many relationship
    /// through the TournamentStage join entity, enabling tournaments to contain multiple
    /// distinct phases with different rules, formats, and team progression patterns while
    /// maintaining referential integrity and supporting complex tournament scenarios.
    /// </remarks>
    public void Configure(EntityTypeBuilder<Tournament> builder) => builder.HasMany<TournamentStage, Tournament, Stage, CompetitionId, StageId>();
}
