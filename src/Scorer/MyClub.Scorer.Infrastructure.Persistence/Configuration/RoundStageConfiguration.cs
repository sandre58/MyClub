// -----------------------------------------------------------------------
// <copyright file="RoundStageConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for RoundStage entities, establishing comprehensive
/// mapping for round stage data including display properties, match relationships, and
/// audit tracking for complex round organization within tournament structures.
/// </summary>
/// <remarks>
/// The RoundStageConfiguration provides complete persistence mapping for round stages,
/// which represent subdivisions within tournament rounds such as first leg, second leg,
/// or multiple games in best-of series, enabling sophisticated round organization with
/// detailed match management and comprehensive audit tracking capabilities.
/// </remarks>
internal sealed class RoundStageConfiguration : IEntityTypeConfiguration<RoundStage>
{
    /// <summary>
    /// Configures the RoundStage entity with complete property mapping, match relationships,
    /// and audit properties for comprehensive round stage data persistence.
    /// </summary>
    /// <param name="builder">The entity type builder for RoundStage configuration.</param>
    /// <remarks>
    /// This configuration establishes display name ownership for round stage identification,
    /// auditable properties for comprehensive lifecycle tracking, and match relationships
    /// through join entities for round stage match management, supporting complex round
    /// scenarios with multiple match phases or legs within tournament structures.
    /// </remarks>
    public void Configure(EntityTypeBuilder<RoundStage> builder)
    {
        builder.ConfigureEntity<RoundStage, RoundStageId>();

        builder.OwnsDisplayName(static x => x.DisplayName);
        builder.OwnsAuditableProperties();

        builder.HasManyMatches<RoundStageMatch, RoundStage, RoundStageId>();
    }
}
