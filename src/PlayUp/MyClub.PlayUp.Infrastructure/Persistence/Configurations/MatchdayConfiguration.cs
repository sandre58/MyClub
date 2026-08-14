// -----------------------------------------------------------------------
// <copyright file="MatchdayConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for Stage matchdays.
/// </summary>
internal sealed class MatchdayConfiguration : IEntityTypeConfiguration<Matchday>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Matchday> builder)
    {
        builder.ToTable("matchdays");
        builder.HasKey(matchday => matchday.Id);

        builder.Property(matchday => matchday.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<MatchdayId>());

        builder.Property(matchday => matchday.Number)
            .HasColumnName("number")
            .IsRequired();

        builder.Property<StageId>("stage_id")
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasMany(matchday => matchday.Fixtures)
            .WithOne()
            .HasForeignKey("matchday_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(matchday => matchday.Fixtures)
            .HasField("_fixtures")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
