// -----------------------------------------------------------------------
// <copyright file="ReplaceStageQualificationRulesGOps3EndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReplaceStageQualificationRulesGOps3EndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Put_qualification_intents_when_group_draw_applied_returns_409_AppliedDrawFeedRulesChangeBlockedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupStageWithAppliedDrawAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            QualificationRulesUri(seed.GroupStageId),
            new ReplaceStageQualificationRulesRequest(
            [
                new QualificationIntentRequest(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    1,
                    seed.DownstreamStageId.Value,
                    DestinationForm: true)
            ]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.AppliedDrawFeedRulesChangeBlocked);
    }

    private static Uri QualificationRulesUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/qualification-rules", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<GroupQualSeed> SeedGroupStageWithAppliedDrawAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("G-OPS3-qual-host", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var downstream = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock).Stage;
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(g => g.Id)]).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        PrepareStage.Execute(stage, [stage, downstream], _clock);

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        competitions.Add(competition);
        stages.Add(downstream);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new GroupQualSeed(stage.Id, downstream.Id);
    }

    private sealed record GroupQualSeed(StageId GroupStageId, StageId DownstreamStageId);
}
