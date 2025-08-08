// -----------------------------------------------------------------------
// <copyright file="CupRoundConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for CupRound join entities, establishing
/// the many-to-many relationship mapping between Cup entities and Round entities
/// for knockout tournament round organization and elimination progression.
/// </summary>
/// <remarks>
/// The CupRoundConfiguration provides standardized join entity configuration for
/// cups containing elimination rounds, enabling knockout tournament formats with
/// structured round progression where teams advance through multiple elimination
/// phases until determining a single winner in traditional cup competitions.
/// </remarks>
internal sealed class CupRoundConfiguration : IEntityTypeConfiguration<CupRound>
{
    /// <summary>
    /// Configures the CupRound join entity using the standardized join entity
    /// configuration pattern for Cup-Round relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for CupRound configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join entity pattern establishing composite
    /// primary keys, foreign key relationships with appropriate cascade behaviors, and
    /// index optimization for efficient round retrieval by cup while supporting complex
    /// knockout tournament structures with multiple elimination phases.
    /// </remarks>
    public void Configure(EntityTypeBuilder<CupRound> builder) => builder.ConfigureJoinEntity<CupRound, Cup, Round, CompetitionId, RoundId>();
}
