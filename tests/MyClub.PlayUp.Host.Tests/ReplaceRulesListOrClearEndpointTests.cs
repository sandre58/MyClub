// -----------------------------------------------------------------------
// <copyright file="ReplaceRulesListOrClearEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReplaceRulesListOrClearEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 8, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Put_ambiguous_progression_body_returns_400_and_leaves_rules_unchanged_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedStageWithRulesAsync(factory);
        using var client = factory.CreateClient();
        var uri = RulesUri(stageId, "progression-rules");

        await AssertRejectedAsync(client, uri, "{}");
        await AssertProgressionUnchangedAsync(factory, stageId);

        await AssertRejectedAsync(client, uri, """{"intents":[]}""");
        await AssertProgressionUnchangedAsync(factory, stageId);

        await AssertRejectedAsync(
            client,
            uri,
            """{"clear":true,"intents":[{"order":1,"roundId":"11111111-1111-1111-1111-111111111111","outcome":"Winner","destinationStageId":"22222222-2222-2222-2222-222222222222"}]}""");
        await AssertProgressionUnchangedAsync(factory, stageId);
    }

    [IntegrationFact]
    public async Task Put_clear_true_progression_returns_204_and_clears_rules_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedStageWithRulesAsync(factory);
        using var client = factory.CreateClient();

        using var response = await PutJsonAsync(client, RulesUri(stageId, "progression-rules"), """{"clear":true}""");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stage = await LoadAsync(factory, stageId);
        stage.Regulation.ProgressionRules.Should().BeNull();
        stage.Regulation.QualificationRules.Should().NotBeNull();
        stage.Regulation.PlacementAwardRules.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task Put_ambiguous_qualification_body_returns_400_and_leaves_rules_unchanged_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedStageWithRulesAsync(factory);
        using var client = factory.CreateClient();
        var uri = RulesUri(stageId, "qualification-rules");

        await AssertRejectedAsync(client, uri, "{}");
        await AssertQualificationUnchangedAsync(factory, stageId);

        await AssertRejectedAsync(client, uri, """{"intents":[]}""");
        await AssertQualificationUnchangedAsync(factory, stageId);

        await AssertRejectedAsync(
            client,
            uri,
            """{"clear":true,"intents":[{"intentId":"33333333-3333-3333-3333-333333333333","order":1,"sourceKind":"Overall","positionFrom":1,"positionTo":1,"destinationStageId":"22222222-2222-2222-2222-222222222222"}]}""");
        await AssertQualificationUnchangedAsync(factory, stageId);
    }

    [IntegrationFact]
    public async Task Put_clear_true_qualification_returns_204_and_clears_rules_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedStageWithRulesAsync(factory);
        using var client = factory.CreateClient();

        using var response = await PutJsonAsync(client, RulesUri(stageId, "qualification-rules"), """{"clear":true}""");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stage = await LoadAsync(factory, stageId);
        stage.Regulation.QualificationRules.Should().BeNull();
        stage.Regulation.ProgressionRules.Should().NotBeNull();
        stage.Regulation.PlacementAwardRules.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task Put_ambiguous_placement_award_body_returns_400_and_leaves_rules_unchanged_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedStageWithRulesAsync(factory);
        using var client = factory.CreateClient();
        var uri = RulesUri(stageId, "placement-award-rules");

        await AssertRejectedAsync(client, uri, "{}");
        await AssertPlacementUnchangedAsync(factory, stageId);

        await AssertRejectedAsync(client, uri, """{"paths":[]}""");
        await AssertPlacementUnchangedAsync(factory, stageId);

        await AssertRejectedAsync(
            client,
            uri,
            """{"clear":true,"paths":[{"outcome":"Winner","rank":1,"sourcePairKey":""}]}""");
        await AssertPlacementUnchangedAsync(factory, stageId);
    }

    [IntegrationFact]
    public async Task Put_clear_true_placement_award_returns_204_and_clears_rules_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedStageWithRulesAsync(factory);
        using var client = factory.CreateClient();

        using var response = await PutJsonAsync(client, RulesUri(stageId, "placement-award-rules"), """{"clear":true}""");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stage = await LoadAsync(factory, stageId);
        stage.Regulation.PlacementAwardRules.Should().BeNull();
        stage.Regulation.ProgressionRules.Should().NotBeNull();
        stage.Regulation.QualificationRules.Should().NotBeNull();
    }

    private static async Task AssertRejectedAsync(HttpClient client, Uri uri, string json)
    {
        using var response = await PutJsonAsync(client, uri, json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.RulesReplaceRequiresListOrClear);
    }

    private static async Task<HttpResponseMessage> PutJsonAsync(HttpClient client, Uri uri, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PutAsync(uri, content);
    }

    private static async Task AssertProgressionUnchangedAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        var stage = await LoadAsync(factory, stageId);
        stage.Regulation.ProgressionRules.Should().NotBeNull();
        stage.Regulation.ProgressionRules!.Paths.Should().ContainSingle()
            .Which.SourcePairKey.Should().Be("P1");
    }

    private static async Task AssertQualificationUnchangedAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        var stage = await LoadAsync(factory, stageId);
        stage.Regulation.QualificationRules.Should().NotBeNull();
        stage.Regulation.QualificationRules!.Paths.Should().ContainSingle()
            .Which.Order.Should().Be(1);
    }

    private static async Task AssertPlacementUnchangedAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        var stage = await LoadAsync(factory, stageId);
        stage.Regulation.PlacementAwardRules.Should().NotBeNull();
        var path = stage.Regulation.PlacementAwardRules!.Paths.Should().ContainSingle().Subject;
        path.SourcePairKey.Should().Be("P1");
        path.Rank.Should().Be(1);
    }

    private static async Task<Stage> LoadAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId);
        stage.Should().NotBeNull();
        return stage!;
    }

    private static Uri RulesUri(StageId stageId, string segment) =>
        new($"/stages/{stageId.Value}/{segment}", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<StageId> SeedStageWithRulesAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Rules replace body", _clock)
            .WithTeams("Home FC", "Away FC");
        var source = situation.AddKnockoutStage("QF", "R1", ["S1", "S2"]);
        var destination = situation.AddBareStage("SF");
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(destination.Id))
            ]),
            _clock);
        source.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath("P1", ProgressionOutcome.Winner, 1)
            ]),
            _clock);

        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return source.Id;
    }
}
