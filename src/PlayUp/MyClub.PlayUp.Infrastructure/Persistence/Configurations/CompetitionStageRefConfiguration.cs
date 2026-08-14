// -----------------------------------------------------------------------
// <copyright file="CompetitionStageRefConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for competition_stage_refs. No CLR navigation on Competition.
/// </summary>
internal sealed class CompetitionStageRefConfiguration : IEntityTypeConfiguration<CompetitionStageRef>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompetitionStageRef> builder)
    {
        builder.ToTable("competition_stage_refs");
        builder.HasKey(row => new { row.CompetitionId, row.StageId });

        builder.Property(row => row.CompetitionId)
            .HasColumnName("competition_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<CompetitionId>());

        builder.Property(row => row.StageId)
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property(row => row.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasOne<Competition>()
            .WithMany()
            .HasForeignKey(row => row.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Stage>()
            .WithMany()
            .HasForeignKey(row => row.StageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
