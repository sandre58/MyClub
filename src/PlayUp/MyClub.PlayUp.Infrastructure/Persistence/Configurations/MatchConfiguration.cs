// -----------------------------------------------------------------------
// <copyright file="MatchConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Match aggregate.
/// </summary>
internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(match => match.Id);

        builder.Property(match => match.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<MatchId>());

        builder.Property(match => match.CompetitionId)
            .HasColumnName("competition_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<CompetitionId>());

        builder.Property(match => match.StageId)
            .HasColumnName("stage_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<StageId>());

        builder.Property(match => match.HomeEntryId)
            .HasColumnName("home_entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        builder.Property(match => match.AwayEntryId)
            .HasColumnName("away_entry_id")
            .HasColumnType("uuid")
            .IsRequired()
            .HasConversion(new GuidTypedIdConverter<EntryId>());

        builder.Property(match => match.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(match => match.Result)
            .HasColumnName("result")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(new MatchResultJsonConverter(), MatchResultJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(match => match.RunningScore)
            .HasColumnName("running_score")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(new RunningScoreJsonConverter(), RunningScoreJsonConverter.Comparer)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Ignore(match => match.DomainEvents);
        builder.Metadata.AddIgnored("_domainEvents");

        builder.OwnsMany(match => match.DeclaredParticipations, ConfigureDeclaredParticipations);
        builder.Navigation(nameof(Match.DeclaredParticipations))
            .HasField("_declaredParticipations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Competition>()
            .WithMany()
            .HasForeignKey(match => match.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Stage>()
            .WithMany()
            .HasForeignKey(match => match.StageId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureDeclaredParticipations(OwnedNavigationBuilder<Match, DeclaredParticipation> participations)
    {
        participations.ToTable("match_declared_participations");
        participations.WithOwner().HasForeignKey("match_id");
        participations.HasKey(participation => participation.Id);

        participations.Property(participation => participation.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(new GuidTypedIdConverter<MemberId>());

        participations.Property(participation => participation.Side)
            .HasColumnName("side")
            .HasConversion<int>()
            .IsRequired();

        participations.Property(participation => participation.CompositionStatus)
            .HasColumnName("composition_status")
            .HasConversion<int>()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        participations.Property(participation => participation.JerseyNumber)
            .HasColumnName("jersey_number")
            .IsRequired(false)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        participations.Property<int>("SortOrder")
            .HasColumnName("sort_order")
            .IsRequired();

        participations.HasIndex("match_id", "SortOrder").IsUnique();
    }
}
