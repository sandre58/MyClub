// -----------------------------------------------------------------------
// <copyright file="MediaConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Media.Domain;
using MyClub.Media.Infrastructure.Persistence.Converters;

namespace MyClub.Media.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Media item aggregate.
/// </summary>
internal sealed class MediaConfiguration : IEntityTypeConfiguration<MediaItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.ToTable("media_items", "media");
        builder.HasKey(media => media.Id);

        builder.Property(media => media.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<MediaId>());

        builder.Property(media => media.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(127)
            .IsRequired();

        builder.Property(media => media.ByteSize)
            .HasColumnName("byte_size")
            .IsRequired();

        builder.Property(media => media.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(media => media.OriginalName)
            .HasColumnName("original_name")
            .HasMaxLength(MediaPolicies.MaxOriginalNameLength)
            .IsRequired(false);

        builder.Property(media => media.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(media => media.StorageKey).IsUnique();
    }
}
