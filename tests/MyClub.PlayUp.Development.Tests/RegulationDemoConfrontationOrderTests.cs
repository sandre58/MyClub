// -----------------------------------------------------------------------
// <copyright file="RegulationDemoConfrontationOrderTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Templates;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class RegulationDemoConfrontationOrderTests(DevelopmentPostgresFixture fixture)
{
    [Fact]
    public async Task Regulation_demo_confrontation_segments_follow_round_sort_orderAsync()
    {
        var templateRunner = fixture.Services.GetRequiredService<TemplateRunner>();
        await templateRunner.ResetAndRunAsync([SeedSpec.Parse("regulation-demo")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var db = scope.ServiceProvider.GetRequiredService<PlayUpDbContext>();

        var summary = (await competitions.ListAsync()).Single(c => c.Name.Value == "Démo Règlement");
        var competition = await competitions.GetByIdReadOnlyAsync(summary.Id);
        competition.Should().NotBeNull();

        var loaded = await stages.GetByIdsReadOnlyAsync(
            competition!.StageIds.ToArray(),
            StageLoadProfile.Structure);

        var knockout = loaded.Single(stage => stage.Name.Value == "Phase finale");
        knockout.Rounds.Select(round => round.Name).Should().Equal(
            "Quarts de finale",
            "Demis de finale",
            "Finale");

        var stageId = knockout.Id;
        var sortOrders = await db.Set<Round>()
            .AsNoTracking()
            .Where(round => EF.Property<StageId>(round, "stage_id") == stageId)
            .OrderBy(round => EF.Property<int>(round, "SortOrder"))
            .Select(round => new
            {
                round.Name,
                SortOrder = EF.Property<int>(round, "SortOrder")
            })
            .ToListAsync();

        sortOrders.Select(row => row.Name).Should().Equal(
            "Quarts de finale",
            "Demis de finale",
            "Finale");
        sortOrders.Select(row => row.SortOrder).Should().Equal(0, 1, 2);

        var view = OrganisationViewAssembler.Assemble(competition, loaded);
        var hub = view.Stages.Single(stage => stage.Name == "Phase finale");
        hub.ConfrontationSegments.Should().NotBeNull();
        hub.ConfrontationSegments!.Should().HaveCount(2);
        hub.ConfrontationSegments[0].Rounds.Select(round => round.Name).Should().Equal(
            "Quarts de finale",
            "Demis de finale");
        hub.ConfrontationSegments[1].Rounds.Select(round => round.Name).Should().Equal("Finale");
    }
}
