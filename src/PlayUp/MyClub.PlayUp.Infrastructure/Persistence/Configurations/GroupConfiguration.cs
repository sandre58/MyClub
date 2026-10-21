// -----------------------------------------------------------------------
// <copyright file="GroupConfiguration.cs" company="Stéphane ANDRE">
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
/// EF Core mapping for Stage groups.
/// </summary>
internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");
        builder.HasKey(group => group.Id);

        builder.Property(group => group.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<GroupId>());

        builder.Property(group => group.Name)
            .HasColumnName("name")
            .HasMaxLength(Group.NameMaxLength)
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property<StageId>("stage_id")
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Ignore(group => group.EntryIds);
        builder.Metadata.AddIgnored("_entryIds");
    }
}
