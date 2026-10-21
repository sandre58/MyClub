// -----------------------------------------------------------------------
// <copyright file="FixtureAttachmentRefConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for fixture_attachments rows.
/// </summary>
internal sealed class FixtureAttachmentRefConfiguration : IEntityTypeConfiguration<FixtureAttachmentRef>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FixtureAttachmentRef> builder)
    {
        builder.ToTable("fixture_attachments");
        builder.HasKey(row => new { row.FixtureId, row.LegIndex });

        builder.Property(row => row.FixtureId)
            .HasColumnName("fixture_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<FixtureId>());

        builder.Property(row => row.LegIndex)
            .HasColumnName("leg_index")
            .IsRequired();

        builder.Property(row => row.MatchId)
            .HasColumnName("match_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<MatchId>());

        builder.HasOne<Fixture>()
            .WithMany()
            .HasForeignKey(row => row.FixtureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(row => row.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
