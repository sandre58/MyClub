// -----------------------------------------------------------------------
// <copyright file="SlotConfiguration.cs" company="Stéphane ANDRE">
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
/// EF Core mapping for Stage slots.
/// </summary>
internal sealed class SlotConfiguration : IEntityTypeConfiguration<Slot>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Slot> builder)
    {
        builder.ToTable("slots");

        builder.Property<StageId>("stage_id")
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        // SlotKey binds to Slot(string slotKey); Id is the same value and is set by that ctor.
        builder.Property(slot => slot.SlotKey)
            .HasColumnName("slot_key")
            .HasMaxLength(Slot.SlotKeyMaxLength)
            .IsRequired();

        builder.Ignore(slot => slot.Id);
        builder.HasKey("stage_id", nameof(Slot.SlotKey));

        builder.Property(slot => slot.EntryId)
            .HasColumnName("entry_id")
            .HasColumnType("uuid")
            .IsRequired(false)
            .HasConversion(new GuidTypedIdConverter<EntryId>())
            .UsePropertyAccessMode(PropertyAccessMode.Property);
    }
}
