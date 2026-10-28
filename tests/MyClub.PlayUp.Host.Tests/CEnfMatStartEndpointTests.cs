// -----------------------------------------------------------------------
// <copyright file="CEnfMatStartEndpointTests.cs" company="Stéphane ANDRE">
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
public sealed class CEnfMatStartEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 15, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Materialize_when_draw_obsolete_returns_409_DrawObsoleteForConsumptionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedPreparedGroupsWithPublishedDrawAsync(factory);

        using var client = factory.CreateClient();
        using var shrink = await client.PutAsJsonAsync(
            AffectationUri(seed.StageId),
            new EntryIdsRequest(
            [
                seed.EntryIds[0].Value,
                seed.EntryIds[1].Value,
                seed.EntryIds[2].Value
            ]),
            HostJson.Options);
        shrink.EnsureSuccessStatusCode();

        using var response = await client.PostAsync(MaterializeUri(seed.StageId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.DrawObsoleteForConsumption);
    }

    private static Uri MaterializeUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/matches/materialize", UriKind.Relative);

    private static Uri AffectationUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/affectation", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<GroupsDrawSeed> SeedPreparedGroupsWithPublishedDrawAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = CreateCompetition.Execute("C-ENF-mat-host", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            competition,
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
        var groupA = stage.Groups[0];
        var groupB = stage.Groups[1];
        stage.AssignEntryToGroup(groupA.Id, ids[0]);
        stage.AssignEntryToGroup(groupA.Id, ids[1]);
        stage.AssignEntryToGroup(groupB.Id, ids[2]);
        stage.AssignEntryToGroup(groupB.Id, ids[3]);

        PrepareStage.Execute(stage, [stage], _clock);

        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new GroupsDrawSeed(stage.Id, ids);
    }

    private sealed record GroupsDrawSeed(StageId StageId, EntryId[] EntryIds);
}
