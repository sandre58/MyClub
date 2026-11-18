// -----------------------------------------------------------------------
// <copyright file="CompositionEntryPotsEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompositionEntryPotsEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Get_structure_serializes_composition_entry_pots_only_for_groups_with_potsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var withPots = await SeedAsync(factory, pots: true);
        var withoutPots = await SeedAsync(factory, pots: false);
        using var client = factory.CreateClient();

        using var present = await client.GetAsync($"/competitions/{withPots.CompetitionId}/structure");
        using var absent = await client.GetAsync($"/competitions/{withoutPots.CompetitionId}/structure");

        present.StatusCode.Should().Be(HttpStatusCode.OK);
        absent.StatusCode.Should().Be(HttpStatusCode.OK);

        using var presentJson = JsonDocument.Parse(await present.Content.ReadAsStringAsync());
        using var absentJson = JsonDocument.Parse(await absent.Content.ReadAsStringAsync());
        var presentStage = presentJson.RootElement.GetProperty("stages")[0];
        var absentStage = absentJson.RootElement.GetProperty("stages")[0];

        var rows = presentStage.GetProperty("compositionEntryPots").EnumerateArray().ToArray();
        var compositionIds = presentStage.GetProperty("compositionEntryIds")
            .EnumerateArray()
            .Select(entry => entry.GetGuid())
            .ToArray();
        rows.Select(row => row.GetProperty("entryId").GetGuid()).Should().Equal(compositionIds);
        rows.Select(row => row.GetProperty("entryId").GetGuid()).Should().BeEquivalentTo(withPots.EntryIds);
        rows.Select(row => row.GetProperty("pot").GetInt32()).Should().Equal(1, 1, 2, 2, 3, 3, 4, 4);
        absentStage.TryGetProperty("compositionEntryPots", out _).Should().BeFalse();
    }

    private async Task<Seed> SeedAsync(PlayUpWebApplicationFactory factory, bool pots)
    {
        var competition = CreateCompetition.Execute(pots ? "Pots-http" : "No-pots-http", _clock);
        var ids = Enumerable.Range(0, 8)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        if (pots)
        {
            ReplaceStageDrawRules.Execute(
                stage,
                competition,
                new DrawRules(DrawMode.Random, potRules: new PotRules(4)),
                _clock);
        }

        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().Add(competition);
        scope.ServiceProvider.GetRequiredService<IStageRepository>().Add(stage);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        return new Seed(competition.Id.Value, ids.Select(id => id.Value).ToArray());
    }

    private sealed record Seed(Guid CompetitionId, Guid[] EntryIds);
}
