// -----------------------------------------------------------------------
// <copyright file="ApplyStageTieFormatEditsEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ApplyStageTieFormatEditsEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 18, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Tie_format_edits_returns_204_and_persists_default_and_roundAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var (stageId, roundId) = await SeedCupDraftAsync(factory);

        using var client = factory.CreateClient();
        var body = new ApplyStageTieFormatEditsRequest(
            ReplaceDefault: true,
            Default: new ReplaceStageDefaultTieFormatRequest(
                Clear: false,
                NumberOfLegs: 2,
                HasAwayGoalsRule: true),
            RoundEdits:
            [
                new ApplyStageTieFormatRoundEditRequest(
                    RoundId: roundId.Value,
                    Clear: false,
                    NumberOfLegs: 1,
                    HasExtraTimeRule: true)
            ]);

        using var response = await client.PutAsJsonAsync(TieFormatEditsUri(stageId), body, HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var stage = await stages.GetByIdForUpdateAsync(stageId);
        stage.Should().NotBeNull();
        stage!.Regulation.TieFormat!.NumberOfLegs.Should().Be(TieFormat.TwoLegs);
        stage.Rounds[0].TieFormat!.NumberOfLegs.Should().Be(TieFormat.SingleLeg);
    }

    [IntegrationFact]
    public async Task Tie_format_edits_returns_404_when_stage_missingAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var body = new ApplyStageTieFormatEditsRequest(
            ReplaceDefault: false,
            Default: null,
            RoundEdits: []);

        using var response = await client.PutAsJsonAsync(
            TieFormatEditsUri(StageId.New()),
            body,
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Tie_format_edits_returns_400_when_replace_default_without_payloadAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var (stageId, _) = await SeedCupDraftAsync(factory);

        using var client = factory.CreateClient();
        var body = new ApplyStageTieFormatEditsRequest(
            ReplaceDefault: true,
            Default: null,
            RoundEdits: []);

        using var response = await client.PutAsJsonAsync(TieFormatEditsUri(stageId), body, HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(ApplicationErrorCodes.TieFormatRequired);
    }

    [IntegrationFact]
    public async Task Tie_format_edits_returns_409_when_stage_runningAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var stageId = await SeedRunningCupAsync(factory);

        using var client = factory.CreateClient();
        var body = new ApplyStageTieFormatEditsRequest(
            ReplaceDefault: true,
            Default: new ReplaceStageDefaultTieFormatRequest(Clear: false, NumberOfLegs: 1),
            RoundEdits: []);

        using var response = await client.PutAsJsonAsync(TieFormatEditsUri(stageId), body, HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(StageErrorCodes.StructureLocked);
    }

    private static Uri TieFormatEditsUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/tie-format-edits", UriKind.Relative);

    private static string? GetCode(ProblemDetails? problem) =>
        problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<(StageId StageId, RoundId RoundId)> SeedCupDraftAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Cup TIEF", _clock)
            .WithStructure(StructureIntent.Cup(2));
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        var stage = situation.RequirePrimaryStage();
        return (stage.Id, stage.Rounds[0].Id);
    }

    private async Task<StageId> SeedRunningCupAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Ch Run", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            situation.TrackMatch(match);
        }

        StartStage.Execute(stage, _clock);
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return stage.Id;
    }
}
