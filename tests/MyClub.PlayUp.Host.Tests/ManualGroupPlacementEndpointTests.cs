// -----------------------------------------------------------------------
// <copyright file="ManualGroupPlacementEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ManualGroupPlacementEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Put_and_delete_group_entry_return_204_and_put_is_idempotentAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, applyDraw: false);
        using var client = factory.CreateClient();
        var uri = EntryUri(seed.StageId, seed.GroupId, seed.EntryId);

        using var created = await client.PutAsync(uri, null);
        using var again = await client.PutAsync(uri, null);
        using var cleared = await client.DeleteAsync(uri);

        created.StatusCode.Should().Be(HttpStatusCode.NoContent);
        again.StatusCode.Should().Be(HttpStatusCode.NoContent);
        cleared.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [IntegrationFact]
    public async Task Put_unknown_group_returns_409_GroupNotFoundAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, applyDraw: false);
        using var client = factory.CreateClient();

        using var response = await client.PutAsync(
            EntryUri(seed.StageId, GroupId.New(), seed.EntryId),
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be(StageErrorCodes.GroupNotFound);
    }

    [IntegrationFact]
    public async Task Put_full_group_returns_409_GroupFullAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, applyDraw: false);
        using var client = factory.CreateClient();
        using var first = await client.PutAsync(EntryUri(seed.StageId, seed.GroupId, seed.EntryId), null);
        using var second = await client.PutAsync(EntryUri(seed.StageId, seed.GroupId, seed.SecondEntryId), null);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var response = await client.PutAsync(
            EntryUri(seed.StageId, seed.GroupId, seed.ThirdEntryId),
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be(StageErrorCodes.GroupFull);
    }

    [IntegrationFact]
    public async Task Put_entry_outside_composition_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, applyDraw: false);
        using var client = factory.CreateClient();

        using var response = await client.PutAsync(
            EntryUri(seed.StageId, seed.GroupId, seed.OutsiderId),
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be(StageErrorCodes.GroupAssignmentEntryNotInComposition);
    }

    [IntegrationFact]
    public async Task Put_and_delete_are_refused_when_matches_block_the_placementAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsWithMatchOnFirstGroupAsync(factory);
        using var client = factory.CreateClient();

        using var again = await client.PutAsync(EntryUri(seed.StageId, seed.GroupId, seed.EntryId), null);
        using var intoBusy = await client.PutAsync(EntryUri(seed.StageId, seed.GroupId, seed.ThirdEntryId), null);
        using var intoQuiet = await client.PutAsync(
            EntryUri(seed.StageId, seed.OtherGroupId, seed.ThirdEntryId),
            null);
        using var clearQuiet = await client.DeleteAsync(
            EntryUri(seed.StageId, seed.OtherGroupId, seed.SecondEntryId));

        again.StatusCode.Should().Be(HttpStatusCode.NoContent);
        intoBusy.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(intoBusy)).Should().Be(StageErrorCodes.GroupAssignLockedByGroupMatches);
        intoQuiet.StatusCode.Should().Be(HttpStatusCode.NoContent);
        clearQuiet.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(clearQuiet)).Should().Be(StageErrorCodes.GroupClearLockedByStageMatches);
    }

    [IntegrationFact]
    public async Task Put_when_group_draw_is_applied_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, applyDraw: true);
        using var client = factory.CreateClient();

        using var response = await client.PutAsync(
            EntryUri(seed.StageId, seed.OtherGroupId, seed.ProtectedEntryId),
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
    }

    private static Uri EntryUri(StageId stageId, GroupId groupId, EntryId entryId) =>
        new(
            $"/stages/{stageId.Value}/groups/{groupId.Value}/entries/{entryId.Value}",
            UriKind.Relative);

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        if (problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => raw.ToString(),
        };
    }

    private async Task<GroupSeed> SeedGroupsAsync(PlayUpWebApplicationFactory factory, bool applyDraw)
    {
        var competition = CreateCompetition.Execute(applyDraw ? "G-http-draw" : "G-http", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var outsider = AddEntry.Execute(competition, "Outside", _clock).Id;
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        if (applyDraw)
        {
            ReplaceStageDrawRules.Execute(
                stage,
                competition,
                new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
                _clock);
        }

        stage.ReplaceAffectationAuthoring(ids, _clock);
        EntryId protectedEntry = ids[0];
        var otherGroup = stage.Groups[1].Id;
        if (applyDraw)
        {
            var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
            ConfigureDrawInputs.Execute(
                stage,
                draw.Id,
                DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
            GenerateDrawResolution.Execute(
                stage,
                draw.Id,
                _clock,
                groupTargets: [.. stage.Groups.Select(group => group.Id)]).IsResolved.Should().BeTrue();
            PublishDraw.Execute(stage, draw.Id, _clock);
            ApplyDraw.Execute(stage, draw.Id, _clock);
            var placement = stage.GetDraw(draw.Id).Resolution.GroupResults[0];
            protectedEntry = placement.EntryId;
            otherGroup = stage.Groups.Select(group => group.Id).First(id => !id.Equals(placement.GroupId));
        }

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new GroupSeed(
            stage.Id,
            stage.Groups[0].Id,
            otherGroup,
            ids[0],
            ids[1],
            ids[2],
            outsider,
            protectedEntry);
    }

    private async Task<GroupSeed> SeedGroupsWithMatchOnFirstGroupAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("G-http-match", _clock);
        var ids = Enumerable.Range(0, 5)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[1]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[2]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[3]);
        var matchday = stage.Matchdays.Count == 0 ? stage.AddMatchday(1, _clock) : stage.Matchdays[0];
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, ids[0], ids[1], _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();

        return new GroupSeed(
            stage.Id,
            stage.Groups[0].Id,
            stage.Groups[1].Id,
            ids[0],
            ids[2],
            ids[4],
            ids[4],
            ids[0]);
    }

    private sealed record GroupSeed(
        StageId StageId,
        GroupId GroupId,
        GroupId OtherGroupId,
        EntryId EntryId,
        EntryId SecondEntryId,
        EntryId ThirdEntryId,
        EntryId OutsiderId,
        EntryId ProtectedEntryId);
}
