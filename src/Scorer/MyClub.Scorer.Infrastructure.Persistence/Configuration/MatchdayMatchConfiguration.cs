// -----------------------------------------------------------------------
// <copyright file="MatchdayMatchConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for MatchdayMatch join entities, establishing
/// the many-to-many relationship mapping between Matchday entities and Match entities
/// for structured fixture organization and match scheduling coordination.
/// </summary>
/// <remarks>
/// The MatchdayMatchConfiguration provides standardized join entity configuration for
/// matchdays containing match assignments, enabling structured fixture organization
/// where matches are grouped into logical rounds or game weeks for comprehensive
/// scheduling coordination and broadcasting organization requirements.
/// </remarks>
internal sealed class MatchdayMatchConfiguration : IEntityTypeConfiguration<MatchdayMatch>
{
    /// <summary>
    /// Configures the MatchdayMatch join entity using the standardized join match
    /// configuration pattern for Matchday-Match relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for MatchdayMatch configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join match pattern establishing composite
    /// primary keys, foreign key relationships with appropriate cascade behaviors, and
    /// index optimization for efficient match retrieval by matchday while supporting
    /// structured fixture organization for broadcasting and operational coordination.
    /// </remarks>
    public void Configure(EntityTypeBuilder<MatchdayMatch> builder) => builder.ConfigureJoinMatch<MatchdayMatch, Matchday, MatchdayId>();
}
