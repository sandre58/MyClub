// -----------------------------------------------------------------------
// <copyright file="PenaltyConfiguration.cs" company="Stéphane ANDRE">
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
/// EF Core mapping for Stage-owned Penalty entities.
/// </summary>
internal sealed class PenaltyConfiguration : IEntityTypeConfiguration<Penalty>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Penalty> builder)
    {
        builder.ToTable("penalties");
        builder.HasKey(penalty => penalty.Id);

        builder.Property(penalty => penalty.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<PenaltyId>());

        builder.Property<StageId>("stage_id")
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property(penalty => penalty.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        builder.Property(penalty => penalty.PointsDeducted)
            .HasColumnName("points_deducted")
            .IsRequired();

        builder.Property(penalty => penalty.Reason)
            .HasColumnName("reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();
    }
}
