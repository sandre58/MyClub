// -----------------------------------------------------------------------
// <copyright file="RoundConfiguration.cs" company="Stéphane ANDRE">
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
/// EF Core mapping for Stage rounds.
/// </summary>
internal sealed class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.ToTable("rounds");
        builder.HasKey(round => round.Id);

        builder.Property(round => round.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<RoundId>());

        builder.Property(round => round.Name)
            .HasColumnName("name")
            .HasMaxLength(Round.NameMaxLength)
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(round => round.TieFormat)
            .HasColumnName("tie_format")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(new TieFormatJsonConverter(), TieFormatJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property<StageId>("stage_id")
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasMany(round => round.Fixtures)
            .WithOne()
            .HasForeignKey("round_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(round => round.Fixtures)
            .HasField("_fixtures")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
