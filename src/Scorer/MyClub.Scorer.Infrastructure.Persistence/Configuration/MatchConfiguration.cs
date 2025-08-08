// -----------------------------------------------------------------------
// <copyright file="MatchConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.CompetitionAggregate.Stadiums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchAggregate.MatchEvents;
using MyClub.Scorer.Infrastructure.Persistence.Converters;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for the Match aggregate root, defining complex
/// mapping for match data including opponents, events, and polymorphic team references.
/// This configuration handles the sophisticated structure of football match entities.
/// </summary>
internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    /// <summary>
    /// Configures the Match entity with complete mapping for all match-related data
    /// including basic properties, relationships, and complex owned entity structures.
    /// </summary>
    /// <param name="builder">The entity type builder for Match configuration.</param>
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ConfigureEntity<Match, MatchId>();

        builder.Property(static x => x.OriginDate).IsRequired();
        builder.Property(static x => x.PostponedDate);
        builder.Property(static x => x.IsNeutralStadium).IsRequired();
        builder.Property(static x => x.Status).HasConversion<string>().IsRequired();
        builder.Property(static x => x.AfterExtraTime).IsRequired();
        builder.OwnsAuditableProperties();
        builder.HasOne<Stadium>().WithMany().HasForeignKey(static x => x.StadiumId).OnDelete(DeleteBehavior.SetNull);

        ConfigureOpponent(builder, static x => x.Home);
        ConfigureOpponent(builder, static x => x.Away);

        builder.OwnsMatchFormat(static x => x.Format);
        builder.OwnsMatchRules(static x => x.Rules);
    }

    /// <summary>
    /// Configures match opponent owned entities with comprehensive event collection mapping.
    /// This method establishes the complex structure for team performance data within matches.
    /// </summary>
    /// <param name="builder">The entity type builder for Match configuration.</param>
    /// <param name="expression">Expression identifying the opponent property (Home or Away).</param>
    private static void ConfigureOpponent(EntityTypeBuilder<Match> builder, Expression<Func<Match, MatchOpponent?>> expression)
    {
        var expressionName = expression.GetMemberName();
        builder.OwnsOne(expression, x =>
        {
            x.ToTable<Match>();
            x.Property(o => o.Team).HasConversion<TeamReferenceConverter>().HasColumnName($"{expressionName}{nameof(MatchOpponent.Team)}");

            x.Property(mo => mo.IsWithdrawn).HasColumnName($"{expressionName}{nameof(MatchOpponent.IsWithdrawn)}");

            x.OwnsMany<Match, MatchOpponent, Goal, MatchEventId, MatchId>(
                mo => mo.MatchId,
                g => g.MatchId,
                y =>
                {
                    y.Property(z => z.Minute);
                    y.Property(z => z.Type).HasConversion<string>();
                    y.Property(z => z.ScorerId).HasNullableStronglyTypedIdConversion().IsRequired(false);
                    y.Property(z => z.AssistId).HasNullableStronglyTypedIdConversion().IsRequired(false);
                },
                prefixTable: expressionName);

            x.OwnsMany<Match, MatchOpponent, Card, MatchEventId, MatchId>(
                mo => mo.MatchId,
                c => c.MatchId,
                y =>
                {
                    y.Property(c => c.Minute);
                    y.Property(c => c.Color).IsRequired();
                    y.Property(c => c.Infraction);
                    y.Property(c => c.PlayerId).HasNullableStronglyTypedIdConversion().IsRequired(false);
                    y.Property(c => c.Description);
                },
                prefixTable: expressionName);

            x.OwnsMany<Match, MatchOpponent, PenaltyShootout, PenaltyShootoutId, MatchId>(
                mo => mo.MatchId,
                ps => ps.MatchId,
                y =>
                {
                    y.Property(ps => ps.Result).HasConversion<string>();
                    y.Property(ps => ps.TakerId).HasNullableStronglyTypedIdConversion().IsRequired(false);
                },
                nameof(MatchOpponent.Shootout),
                expressionName);
        });
    }
}
