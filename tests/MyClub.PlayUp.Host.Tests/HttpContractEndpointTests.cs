// -----------------------------------------------------------------------
// <copyright file="HttpContractEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Minimal HTTP contract tests (string enums + named response DTOs).
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class HttpContractEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Workspace_status_is_serialized_as_json_string_not_numberAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest($"Contract {Guid.CreateVersion7():N}"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await createResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var status = document.RootElement.GetProperty("status");
        status.ValueKind.Should().Be(JsonValueKind.String);
        status.GetString().Should().Be("Draft");
    }

    [IntegrationFact]
    public async Task Materialize_returns_named_contract_shapeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "MatContract");
        await AddEntriesAsync(client, competitionId, 4);
        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/structure",
            new ConfigureStructureRequest("Championship"));
        structureResponse.EnsureSuccessStatusCode();
        var org = (await structureResponse.Content.ReadFromJsonAsync<ConfigureStructureResponse>(HostJson.Options))!
            .Structure;
        var stageId = org.Format.PrimaryStageId!.Value;

        using (var prepare = await HostStageHttpHelpers.PostPrepareAsync(client, stageId))
        {
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var materializeResponse = await client.PostAsync(
            $"/stages/{stageId}/matches/materialize",
            content: null);
        materializeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await materializeResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("createdCount").GetInt32().Should().Be(6);
        root.GetProperty("alreadyComplete").GetBoolean().Should().BeFalse();
        root.GetProperty("attachedMatchIds").GetArrayLength().Should().Be(6);

        var typed = JsonSerializer.Deserialize<MaterializeMatchesResponse>(json, HostJson.Options);
        typed.Should().NotBeNull();
        typed.CreatedCount.Should().Be(6);
        typed.AttachedMatchIds.Should().HaveCount(6);
        typed.AlreadyComplete.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task Structure_stage_hub_projects_lifecycle_wire_shapeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var competitionId = await CreateCompetitionAsync(client, "LifecycleContract");
        await AddEntriesAsync(client, competitionId, 4);
        using var structureResponse = await client.PostAsJsonAsync(
            $"/competitions/{competitionId}/structure",
            new ConfigureStructureRequest("Championship"));
        structureResponse.EnsureSuccessStatusCode();

        using var getResponse = await client.GetAsync($"/competitions/{competitionId}/structure");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await getResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var stage = document.RootElement.GetProperty("stages")[0];
        stage.GetProperty("status").GetString().Should().Be("Draft");

        var lifecycle = stage.GetProperty("lifecycle");
        var transition = lifecycle.GetProperty("transition");
        transition.GetProperty("code").GetString().Should().Be(OverviewAssembler.ActionPrepareStage);
        transition.GetProperty("enabled").GetBoolean().Should().BeTrue();

        lifecycle.TryGetProperty("prepareChecklist", out _).Should().BeFalse();

        lifecycle.TryGetProperty("matchProgress", out var progress).Should().BeTrue();
        progress.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [IntegrationFact]
    public async Task Complete_stage_returns_204_and_structure_matchProgress_canCompleteAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 10, 18, 0, 0, TimeSpan.Zero));
        var stageId = await SeedRunningFinishedStageAsync(factory, clock);

        using var client = factory.CreateClient();
        using var completeResponse = await client.PostAsync(
            $"/stages/{stageId}/complete",
            content: null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [IntegrationFact]
    public async Task Complete_stage_when_open_matches_returns_409_with_code_and_reasonsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var clock = new FakeClock(new DateTimeOffset(2026, 10, 10, 18, 30, 0, TimeSpan.Zero));
        var stageId = await SeedRunningScheduledStageAsync(factory, clock);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync($"/stages/{stageId}/complete", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetProblemCode(problem).Should().Be(ApplicationErrorCodes.StageCompletionBlocked);
        GetProblemReasons(problem).Should().Contain(CompleteStage.ReasonOpenMatches);
    }

    [IntegrationFact]
    public async Task Apply_qualification_returns_named_contract_shapeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var (leagueId, _) = await QualificationContractSeed.CreateAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/stages/{leagueId}/qualification/apply",
            content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("appliedCount").GetInt32().Should().BeGreaterThan(0);
        var assignments = root.GetProperty("assignments");
        assignments.GetArrayLength().Should().BeGreaterThan(0);
        var first = assignments[0];
        first.TryGetProperty("stageId", out _).Should().BeTrue();
        first.TryGetProperty("entryId", out _).Should().BeTrue();
        first.TryGetProperty("slotKey", out _).Should().BeFalse();

        var typed = JsonSerializer.Deserialize<QualificationApplyResponse>(json, HostJson.Options);
        typed.Should().NotBeNull();
        typed.AppliedCount.Should().Be(typed.Assignments.Count);
        typed.Assignments.Should().Contain(a => a.EntryId != Guid.Empty);
    }

    private static async Task<Guid> CreateCompetitionAsync(HttpClient client, string prefix)
    {
        using var createResponse = await client.PostAsJsonAsync(
            "/competitions",
            new CreateCompetitionRequest($"{prefix} {Guid.CreateVersion7():N}"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        return created!.Id;
    }

    private static async Task AddEntriesAsync(HttpClient client, Guid competitionId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            using var response = await client.PostAsJsonAsync(
                $"/competitions/{competitionId}/entries",
                new AddEntryRequest($"Team {i}"));
            response.EnsureSuccessStatusCode();
        }
    }

    private static string? GetProblemCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private static IReadOnlyList<string> GetProblemReasons(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("reasons", out var raw) || raw is null
            ? []
            : raw switch
            {
                JsonElement { ValueKind: JsonValueKind.Array } element => [
                    .. element.EnumerateArray().Select(item => item.GetString()!)
                ]!,
                IEnumerable<string> texts => [.. texts],
                _ => []
            };

    private static async Task<Guid> SeedRunningFinishedStageAsync(
        PlayUpWebApplicationFactory factory,
        FakeClock clock)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Contract Complete"), SampleRegulations.Standard(), clock);
        var home = competition.AddEntry(TeamId.New(), "A", clock);
        var away = competition.AddEntry(TeamId.New(), "B", clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), clock);
        stage.AddMatchday(1, clock);
        competition.AddStage(stage.Id, clock);
        PrepareStage.Execute(stage, [stage], clock);
        StartStage.Execute(stage, clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, clock);
        match.Start(clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), clock);
        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return stage.Id.Value;
    }

    private static async Task<Guid> SeedRunningScheduledStageAsync(
        PlayUpWebApplicationFactory factory,
        FakeClock clock)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Contract Open"), SampleRegulations.Standard(), clock);
        var home = competition.AddEntry(TeamId.New(), "A", clock);
        var away = competition.AddEntry(TeamId.New(), "B", clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), clock);
        stage.AddMatchday(1, clock);
        competition.AddStage(stage.Id, clock);
        PrepareStage.Execute(stage, [stage], clock);
        StartStage.Execute(stage, clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, clock);
        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return stage.Id.Value;
    }
}
