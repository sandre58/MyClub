// -----------------------------------------------------------------------
// <copyright file="RoundStageMatchConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for RoundStageMatch join entities, establishing
/// the many-to-many relationship mapping between RoundStage entities and Match entities
/// for complex round organization with multiple match phases or legs.
/// </summary>
/// <remarks>
/// The RoundStageMatchConfiguration provides standardized join entity configuration for
/// round stages containing multiple matches, enabling sophisticated round formats such as
/// home-and-away ties, best-of series, or multi-leg knockout rounds commonly used in
/// professional football tournament structures.
/// </remarks>
internal sealed class RoundStageMatchConfiguration : IEntityTypeConfiguration<RoundStageMatch>
{
    /// <summary>
    /// Configures the RoundStageMatch join entity using the standardized join match
    /// configuration pattern for RoundStage-Match relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for RoundStageMatch configuration.</param>
    public void Configure(EntityTypeBuilder<RoundStageMatch> builder) => builder.ConfigureJoinMatch<RoundStageMatch, RoundStage, RoundStageId>();
}
