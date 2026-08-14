// -----------------------------------------------------------------------
// <copyright file="GroupEntryRefConfiguration.cs" company="Stéphane ANDRE">
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
/// EF Core mapping for ordered group entry rows.
/// </summary>
internal sealed class GroupEntryRefConfiguration : IEntityTypeConfiguration<GroupEntryRef>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GroupEntryRef> builder)
    {
        builder.ToTable("group_entries");
        builder.HasKey(row => new { row.GroupId, row.EntryId });

        builder.Property(row => row.GroupId)
            .HasColumnName("group_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<GroupId>());

        builder.Property(row => row.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        builder.Property(row => row.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(row => row.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
