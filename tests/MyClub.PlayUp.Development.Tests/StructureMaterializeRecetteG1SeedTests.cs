// -----------------------------------------------------------------------
// <copyright file="StructureMaterializeRecetteG1SeedTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// RECETTE G1 seed invariant: after <c>structure-materialize-recette</c>, the groups stage
/// is <see cref="StageStatus.Ready"/> and has no attached matches.
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class StructureMaterializeRecetteG1SeedTests(DevelopmentPostgresFixture fixture)
{
    [Fact]
    public async Task G1_after_structure_materialize_recette_is_ready_with_zero_attached_matchesAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("structure-materialize-recette")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

        var summary = (await competitions.ListAsync())
            .Should()
            .ContainSingle(competition => competition.Name.Value.StartsWith("RECETTE G1", StringComparison.Ordinal))
            .Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();

        var stage = (await stages.GetByIdsForUpdateAsync(competition!.StageIds))
            .Should()
            .ContainSingle()
            .Subject;
        stage.Status.Should().Be(StageStatus.Ready);

        var attachedMatches = await matches.ListByStageForUpdateAsync(stage.Id);
        attachedMatches.Should().NotBeNull();
        attachedMatches.Count.Should().Be(0);
        attachedMatches.Should().BeEmpty();
    }
}
