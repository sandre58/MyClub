// -----------------------------------------------------------------------
// <copyright file="StageConfiguration.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Infrastructure.Persistence.Extensions;
using MyClub.Scorer.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Infrastructure.Persistence.Extensions;

namespace MyClub.Scorer.Infrastructure.Persistence.Configuration;

/// <summary>
/// Entity Framework Core configuration for the Stage aggregate root hierarchy,
/// implementing table-per-hierarchy inheritance strategy for different stage types.
/// This configuration establishes the foundation for multiphase tournament management.
/// </summary>
/// <remarks>
/// The StageConfiguration uses table-per-hierarchy (TPH) inheritance mapping to handle
/// the polymorphic Stage hierarchy including ChampionshipStage, GroupStage, and
/// KnockoutStage types. This approach enables efficient tournament organization while
/// maintaining type safety and supporting complex multi-stage competition scenarios.
/// </remarks>
internal sealed class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    /// <summary>
    /// Configures the Stage entity hierarchy with inheritance discriminator, shared
    /// properties, value object ownership, and hierarchical stage relationships.
    /// </summary>
    /// <param name="builder">The entity type builder for Stage configuration.</param>
    public void Configure(EntityTypeBuilder<Stage> builder)
    {
        builder.UseTphMappingStrategy();

        builder.HasDiscriminator<StageType>("StageType")
               .HasValue<ChampionshipStage>(StageType.Championship)
               .HasValue<GroupStage>(StageType.Groups)
               .HasValue<KnockoutStage>(StageType.Knockout)
        ;

        builder.ConfigureEntity<Stage, StageId>();

        builder.OwnsDisplayName(static x => x.DisplayName);
        builder.OwnsMatchRules(static x => x.Rules);
        builder.OwnsMatchFormat(static x => x.MatchFormat);
        builder.Property(static x => x.IsConsolation).IsRequired();
        builder.OwnsAuditableProperties();
        builder.HasOne<Stage>().WithMany().HasForeignKey(static x => x.AncestorStageId).OnDelete(DeleteBehavior.Restrict);

        builder.HasManyTeams<Stage, StageId, StageTeam>();
    }
}
