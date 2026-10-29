// -----------------------------------------------------------------------
// <copyright file="RedoDrawEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// D4B: the <c>/redo</c> endpoint runs Cancel → Release → preview a new resolution as a single
/// persisted unit of work. These Host-level tests prove the atomicity that the Application unit
/// tests cannot: a block leaves the database completely unchanged (no half-cancelled draw).
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class RedoDrawEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 29, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Redo_returns_200_and_persists_cancel_release_and_a_new_draft_drawAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedSlotDrawAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DrawGenerationDto>(HostJson.Options);
        body.Should().NotBeNull();
        body!.DrawId.Should().NotBe(seed.DrawId.Value);
        body.IsResolved.Should().BeTrue();
        body.IsNoSolution.Should().BeFalse();
        body.Status.Should().Be(DrawStatus.Draft);
        body.ResolutionState.Should().Be(DrawResolutionState.Resolved);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage!.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Cancelled);
        stage.GetDraw(new DrawId(body.DrawId)).Status.Should().Be(DrawStatus.Draft);
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        stage.FindSlot("S2")!.EntryId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Redo_when_a_direct_assignment_is_present_returns_409_and_changes_nothingAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedSlotDrawAsync(factory, withUnrelatedDirectAssignment: true);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.DrawLaunchDirectAssignmentPresent);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage!.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Published);
        stage.FindSlot("S1")!.EntryId.Should().Be(seed.EntryA);
        stage.FindSlot("S2")!.EntryId.Should().Be(seed.EntryB);
        stage.Draws.Should().ContainSingle();
    }

    [IntegrationFact]
    public async Task Redo_when_stage_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RedoUri(StageId.New(), DrawId.New()), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    private static Uri RedoUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/redo", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<SlotDrawSeed> SeedAppliedSlotDrawAsync(
        PlayUpWebApplicationFactory factory,
        bool withUnrelatedDirectAssignment = false)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Redo Draw Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.SeedEntryRoundBracketPairs();
        ReplaceStageDrawRules.Execute(stage, competition, new DrawRules(DrawMode.Random), _clock);
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var authoredEntries = new List<EntryId> { entryA, entryB };

        EntryId? directEntry = null;
        if (withUnrelatedDirectAssignment)
        {
            stage.AddSlot("S3");
            directEntry = EntryId.New();
            authoredEntries.Add(directEntry.Value);
        }

        stage.ReplaceAffectationAuthoring(authoredEntries, _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets: ["S1", "S2"])
            .IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        if (directEntry is { } entry)
        {
            stage.AssignEntryToSlot("S3", entry);
        }

        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new SlotDrawSeed(stage.Id, draw.Id, entryA, entryB);
    }

    private sealed record SlotDrawSeed(StageId StageId, DrawId DrawId, EntryId EntryA, EntryId EntryB);
}
