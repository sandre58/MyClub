// -----------------------------------------------------------------------
// <copyright file="CompetitionConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Competition aggregate.
/// </summary>
internal sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.ToTable("competitions");
        builder.HasKey(competition => competition.Id);

        builder.Property(competition => competition.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<CompetitionId>());

        builder.Property(competition => competition.Name)
            .HasColumnName("name")
            .HasMaxLength(CompetitionName.MaxLength)
            .IsRequired()
            .HasConversion(name => name.Value, value => new CompetitionName(value));

        builder.Property(competition => competition.ShortName)
            .HasColumnName("short_name")
            .HasMaxLength(ShortName.MaxLength)
            .IsRequired(false)
            .HasConversion(
                name => name == null ? null : name.Value,
                value => ShortName.Create(value))
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(competition => competition.LogoUri)
            .HasColumnName("logo_uri")
            .HasMaxLength(LogoUri.MaxLength)
            .IsRequired(false)
            .HasConversion(
                uri => uri == null ? null : uri.Value,
                value => LogoUri.Create(value))
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(competition => competition.ScheduledStart)
            .HasColumnName("scheduled_start")
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(competition => competition.ScheduledEnd)
            .HasColumnName("scheduled_end")
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(competition => competition.Regulation)
            .HasColumnName("regulation")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(new RegulationJsonConverter(), RegulationJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(competition => competition.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(competition => competition.CompletionMode)
            .HasColumnName("completion_mode")
            .HasConversion<int?>()
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Ignore(competition => competition.StageIds);
        builder.Ignore(competition => competition.DomainEvents);
        builder.Metadata.AddIgnored("_stageIds");
        builder.Metadata.AddIgnored("_domainEvents");

        builder.OwnsMany<CompetitionEntry>(nameof(Competition.Entries), ConfigureEntries);
        builder.Navigation(nameof(Competition.Entries))
            .HasField("_entries")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureEntries(OwnedNavigationBuilder<Competition, CompetitionEntry> entries)
    {
        entries.ToTable("competition_entries");
        entries.WithOwner().HasForeignKey("competition_id");
        entries.HasKey(entry => entry.Id);

        entries.Property(entry => entry.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        entries.Property(entry => entry.TeamId)
            .HasColumnName("team_id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<TeamId>());

        entries.Property(entry => entry.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(CompetitionEntry.DisplayNameMaxLength)
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        entries.Property(entry => entry.ShortName)
            .HasColumnName("short_name")
            .HasMaxLength(ShortName.MaxLength)
            .IsRequired(false)
            .HasConversion(
                name => name == null ? null : name.Value,
                value => ShortName.Create(value))
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        entries.Property(entry => entry.LogoUri)
            .HasColumnName("logo_uri")
            .HasMaxLength(LogoUri.MaxLength)
            .IsRequired(false)
            .HasConversion(
                uri => uri == null ? null : uri.Value,
                value => LogoUri.Create(value))
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        entries.Property(entry => entry.PrimaryColor)
            .HasColumnName("primary_color")
            .HasMaxLength(7)
            .IsRequired(false)
            .HasConversion(
                color => color == null ? null : color.Value,
                value => TeamColor.Create(value))
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        entries.Property(entry => entry.SecondaryColor)
            .HasColumnName("secondary_color")
            .HasMaxLength(7)
            .IsRequired(false)
            .HasConversion(
                color => color == null ? null : color.Value,
                value => TeamColor.Create(value))
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        entries.Property(entry => entry.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        entries.Ignore(entry => entry.IsOccupying);

        entries.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        entries.HasIndex("competition_id", "SortOrder").IsUnique();
    }
}
