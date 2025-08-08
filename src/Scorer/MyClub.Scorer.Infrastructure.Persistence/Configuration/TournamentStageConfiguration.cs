// -----------------------------------------------------------------------
// <copyright file="TournamentStageConfiguration.cs" company="Stéphane ANDRE">
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
/// Entity Framework Core configuration for TournamentStage join entities, establishing
/// the many-to-many relationship mapping between Tournament competitions and Stage entities
/// for multiphase tournament organization and management.
/// </summary>
/// <remarks>
/// The TournamentStageConfiguration provides standardized join entity configuration for
/// tournaments containing multiple stages, enabling complex tournament formats that combine
/// group phases, knockout rounds, and championship stages within unified tournament structures
/// such as World Cup or Champions League formats.
/// </remarks>
internal sealed class TournamentStageConfiguration : IEntityTypeConfiguration<TournamentStage>
{
    /// <summary>
    /// Configures the TournamentStage join entity using the standardized join entity
    /// configuration pattern for Tournament-Stage relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for TournamentStage configuration.</param>
    public void Configure(EntityTypeBuilder<TournamentStage> builder) => builder.ConfigureJoinEntity<TournamentStage, Tournament, Stage, CompetitionId, StageId>();
}
