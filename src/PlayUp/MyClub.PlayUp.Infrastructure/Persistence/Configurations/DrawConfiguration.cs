// -----------------------------------------------------------------------
// <copyright file="DrawConfiguration.cs" company="Stéphane ANDRE">
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
/// EF Core mapping for Stage-owned Draw entities.
/// </summary>
internal sealed class DrawConfiguration : IEntityTypeConfiguration<Draw>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Draw> builder)
    {
        builder.ToTable("draws");
        builder.HasKey(draw => draw.Id);

        builder.Property(draw => draw.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<DrawId>());

        builder.Property<StageId>("stage_id")
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property(draw => draw.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(draw => draw.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(draw => draw.Inputs)
            .HasColumnName("inputs")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(new DrawInputsJsonConverter(), DrawInputsJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(draw => draw.Resolution)
            .HasColumnName("resolution")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(new DrawResolutionJsonConverter(), DrawResolutionJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();
    }
}
