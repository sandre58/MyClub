// -----------------------------------------------------------------------
// <copyright file="CompetitionConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for the Competition aggregate root hierarchy,
/// implementing table-per-hierarchy inheritance strategy for different competition types.
/// This configuration establishes the foundation for all football competition entities.
/// </summary>
internal sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    /// <summary>
    /// Configures the Competition entity hierarchy with inheritance discriminator,
    /// shared properties, value object ownership, and team/stadium relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for Competition configuration.</param>
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.UseTphMappingStrategy();

        builder.HasDiscriminator<CompetitionType>(nameof(CompetitionType))
               .HasValue<League>(CompetitionType.League)
               .HasValue<Cup>(CompetitionType.Cup)
               .HasValue<Tournament>(CompetitionType.Tournament)
        ;

        builder.ConfigureEntity<Competition, CompetitionId>();

        builder.OwnsDisplayName(static x => x.DisplayName);
        builder.OwnsMatchRules(static x => x.MatchRules);
        builder.OwnsMatchFormat(static x => x.MatchFormat);
        builder.OwnsAuditableProperties();

        builder.HasMany(static x => x.Teams).WithOne().HasForeignKey($"{nameof(Competition)}Id").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(static x => x.Stadiums).WithOne().HasForeignKey($"{nameof(Competition)}Id").OnDelete(DeleteBehavior.Cascade);
    }
}
