// -----------------------------------------------------------------------
// <copyright file="CupConfiguration.cs" company="Stéphane ANDRE">
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
/// Entity Framework Core configuration for Cup entities, establishing the
/// many-to-many relationship with Round entities for knockout tournament organization.
/// This configuration enables cup competitions with multiple elimination rounds.
/// </summary>
/// <remarks>
/// The CupConfiguration extends the base Competition configuration with cup-specific
/// round relationship management, enabling knockout tournament formats with multiple
/// elimination rounds where teams progress through structured rounds until only
/// one winner remains in traditional cup competition formats.
/// </remarks>
internal sealed class CupConfiguration : IEntityTypeConfiguration<Cup>
{
    /// <summary>
    /// Configures the Cup entity with round relationship management using the
    /// standardized many-to-many join entity pattern for knockout tournaments.
    /// </summary>
    /// <param name="builder">The entity type builder for Cup configuration.</param>
    /// <remarks>
    /// This configuration establishes the Cup-Round many-to-many relationship through
    /// the CupRound join entity, enabling cups to contain multiple elimination rounds
    /// with different formats and team progression patterns while maintaining referential
    /// integrity and supporting complex knockout tournament scenarios.
    /// </remarks>
    public void Configure(EntityTypeBuilder<Cup> builder) => builder.HasMany<CupRound, Cup, Round, CompetitionId, RoundId>();
}
