// -----------------------------------------------------------------------
// <copyright file="RoundTeamConfiguration.cs" company="Stéphane ANDRE">
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
/// Entity Framework Core configuration for RoundTeam join entities, establishing
/// the many-to-many relationship mapping between Round entities and team references
/// for knockout tournament round organization and team progression tracking.
/// </summary>
/// <remarks>
/// The RoundTeamConfiguration provides standardized join entity configuration for
/// rounds containing team assignments, enabling knockout tournament round management
/// where teams participate in elimination rounds and progress through structured
/// tournament brackets based on match results and qualification outcomes.
/// </remarks>
internal sealed class RoundTeamConfiguration : IEntityTypeConfiguration<RoundTeam>
{
    /// <summary>
    /// Configures the RoundTeam join entity using the standardized join team
    /// configuration pattern for Round-Team relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for RoundTeam configuration.</param>
    /// <remarks>
    /// This configuration applies the standard join team pattern establishing composite
    /// primary keys, foreign key relationships, team reference conversion, and index
    /// optimization for efficient team retrieval by round while supporting polymorphic
    /// team references for complex knockout tournament progression scenarios.
    /// </remarks>
    public void Configure(EntityTypeBuilder<RoundTeam> builder) => builder.ConfigureJoinTeam<RoundTeam, Round, RoundId>();
}
