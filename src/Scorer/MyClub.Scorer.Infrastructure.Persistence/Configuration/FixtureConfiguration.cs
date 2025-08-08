// -----------------------------------------------------------------------
// <copyright file="FixtureConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Converters;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for Fixture entities, establishing mapping
/// for tournament fixture data including polymorphic team reference handling and
/// audit property management for comprehensive fixture tracking.
/// </summary>
internal sealed class FixtureConfiguration : IEntityTypeConfiguration<Fixture>
{
    /// <summary>
    /// Configures the Fixture entity with polymorphic team reference conversion
    /// and auditable properties for comprehensive fixture data persistence.
    /// </summary>
    /// <param name="builder">The entity type builder for Fixture configuration.</param>
    public void Configure(EntityTypeBuilder<Fixture> builder)
    {
        builder.ConfigureEntity<Fixture, FixtureId>();

        builder.Property(static o => o.Team1).HasConversion<TeamReferenceConverter>();
        builder.Property(static o => o.Team2).HasConversion<TeamReferenceConverter>();
        builder.OwnsAuditableProperties();
    }
}
