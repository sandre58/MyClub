// -----------------------------------------------------------------------
// <copyright file="SetGroupMembershipEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class SetGroupMembershipEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Stale_expected_returns_409_and_keeps_concurrent_membershipAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 4, entryCount: 4, members: [0, 1]);
        using var client = factory.CreateClient();
        var current = seed.Entries.Take(2).Select(id => id.Value).ToArray();

        using var first = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest([seed.Entries[2].Value], current));
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var stale = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest([seed.Entries[1].Value], current));

        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(stale)).Should().Be(ApplicationErrorCodes.GroupMembershipStale);
        var reloaded = await ReloadAsync(factory, seed.StageId);
        reloaded.GetGroup(seed.GroupId).EntryIds.Should().Equal(seed.Entries[2]);
    }

    [IntegrationFact]
    public async Task Expected_set_in_another_order_does_not_conflictAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 4, entryCount: 4, members: [0, 1]);
        using var client = factory.CreateClient();
        var current = seed.Entries.Take(2).Select(id => id.Value).ToArray();

        using var response = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest(
                [seed.Entries[0].Value, seed.Entries[1].Value, seed.Entries[2].Value],
                [current[1], current[0]]));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [IntegrationFact]
    public async Task Noop_with_stale_expected_returns_204_without_row_changesAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 4, entryCount: 4, members: [0, 1]);
        using var client = factory.CreateClient();
        var current = new[] { seed.Entries[2].Value, seed.Entries[3].Value };

        using var response = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest(
                [seed.Entries[0].Value, seed.Entries[1].Value],
                current));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var rows = await ReadSortOrdersAsync(seed.GroupId);
        rows.Should().Equal(0, 1);
    }

    [IntegrationFact]
    public async Task Noop_with_active_draw_returns_204_without_changing_sort_orderAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedWithDraftDrawAsync(factory, entryCount: 4, members: [0, 1]);
        using var client = factory.CreateClient();
        var members = seed.Entries.Take(2).Select(id => id.Value).ToArray();

        using var response = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest(members, members));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var rows = await ReadSortOrdersAsync(seed.GroupId);
        rows.Should().Equal(0, 1);
    }

    [IntegrationFact]
    public async Task Noop_while_running_returns_structure_lockedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 3, members: [0], running: true);
        using var client = factory.CreateClient();
        var member = new[] { seed.Entries[0].Value };

        using var response = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest(member, member));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be(StageErrorCodes.StructureLocked);
    }

    [IntegrationFact]
    public async Task Two_concurrent_membership_puts_return_204_and_409_without_500Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 4, members: []);
        using var client = factory.CreateClient();

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => client.PutAsJsonAsync(
                MembershipUri(seed),
                new SetGroupEntriesRequest(
                    [seed.Entries[0].Value, seed.Entries[1].Value],
                    [])),
            () => client.PutAsJsonAsync(
                MembershipUri(seed),
                new SetGroupEntriesRequest(
                    [seed.Entries[2].Value, seed.Entries[3].Value],
                    [])));

        responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
            [HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
        responses.Should().OnlyContain(response => (int)response.StatusCode < 500);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Removing_applied_draw_entry_returns_409_and_writes_nothingAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedWithAppliedDrawAsync(factory);
        using var client = factory.CreateClient();
        var current = seed.Entries.Take(2).Select(id => id.Value).ToArray();
        var next = new[] { seed.Entries[1].Value };

        using var response = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest(next, current));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be(ApplicationErrorCodes.AppliedDrawGroupPlacementProtected);
        var reloaded = await ReloadAsync(factory, seed.StageId);
        reloaded.GetGroup(seed.GroupId).EntryIds.Should().HaveCount(2);
    }

    [IntegrationFact]
    public async Task Validation_returns_400_for_invalid_membership_requestsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 4, entryCount: 4, members: [0, 1]);
        using var client = factory.CreateClient();
        var current = seed.Entries.Take(2).Select(id => id.Value).ToArray();

        using var missingExpected = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest([seed.Entries[2].Value], null));
        using var clearWithoutExpected = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest([], null));
        using var duplicate = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest(
                [seed.Entries[0].Value, seed.Entries[0].Value],
                current));
        using var emptyGuid = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest([Guid.Empty], current));
        using var tooMany = await client.PutAsJsonAsync(
            MembershipUri(seed),
            new SetGroupEntriesRequest([.. Enumerable.Repeat(seed.Entries[0].Value, 65)], current));

        missingExpected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        clearWithoutExpected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        emptyGuid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        tooMany.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [IntegrationFact]
    public async Task Structure_hub_exposes_SetGroupEntries_in_draft_and_readyAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 3, members: [0]);
        using var client = factory.CreateClient();

        using var draft = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/structure");
        draft.StatusCode.Should().Be(HttpStatusCode.OK);
        using var draftDoc = await JsonDocument.ParseAsync(await draft.Content.ReadAsStreamAsync());
        draftDoc.RootElement.GetProperty("stages")[0].GetProperty("actions")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Should()
            .Contain(StructureViewAssembler.ActionSetGroupEntries);

        using (var scope = factory.Services.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stage = await stages.GetByIdForUpdateAsync(seed.StageId);
            stage!.Prepare(_clock);
            await unitOfWork.SaveChangesAsync();
        }

        using var ready = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/structure");
        using var readyDoc = await JsonDocument.ParseAsync(await ready.Content.ReadAsStreamAsync());
        readyDoc.RootElement.GetProperty("stages")[0].GetProperty("actions")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Should()
            .Contain(StructureViewAssembler.ActionSetGroupEntries);
    }

    private async Task<int[]> ReadSortOrdersAsync(GroupId groupId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT sort_order FROM group_entries WHERE group_id = @groupId ORDER BY sort_order",
            connection);
        command.Parameters.AddWithValue(nameof(groupId), groupId.Value);
        var rows = new List<int>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetInt32(0));
        }

        return [.. rows];
    }

    private async Task<HttpResponseMessage[]> OverlapOnCompetitionLockAsync(
        CompetitionId competitionId,
        Func<Task<HttpResponseMessage>> left,
        Func<Task<HttpResponseMessage>> right)
    {
        var key = CompetitionAdvisoryLock.Key(competitionId);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using (var hold = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection))
        {
            hold.Parameters.AddWithValue("key", key);
            _ = await hold.ExecuteScalarAsync();
        }

        var leftTask = left();
        var rightTask = right();
        var overlapped = false;
        try
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                await using var count = new NpgsqlCommand(
                    "SELECT COUNT(*)::int FROM pg_locks WHERE locktype = 'advisory' AND NOT granted",
                    connection);
                var waiting = (int)(await count.ExecuteScalarAsync() ?? 0);
                if (waiting >= 2)
                {
                    overlapped = true;
                    break;
                }

                await Task.Delay(50);
            }
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
            release.Parameters.AddWithValue("key", key);
            _ = await release.ExecuteScalarAsync();
        }

        var responses = await Task.WhenAll(leftTask, rightTask);
        overlapped.Should().BeTrue("both writers must be waiting on the competition advisory lock");
        return responses;
    }

    private static Uri MembershipUri(Seed seed) =>
        new($"/stages/{seed.StageId.Value}/groups/{seed.GroupId.Value}/membership", UriKind.Relative);

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
            _ => raw.ToString()
        };
    }

    private static async Task<Stage> ReloadAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        return (await stages.GetByIdForUpdateAsync(stageId))!;
    }

    private async Task<Seed> SeedAsync(
        PlayUpWebApplicationFactory factory,
        int places,
        int entryCount,
        int[] members,
        bool running = false)
    {
        var competition = CreateCompetition.Execute($"Membership-{places}-{entryCount}", _clock);
        var ids = Enumerable.Range(0, entryCount)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, places),
            _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        var group = stage.Groups[0];
        foreach (var index in members)
        {
            stage.AssignEntryToGroup(group.Id, ids[index]);
        }

        if (running)
        {
            stage.Prepare(_clock);
            stage.Start(_clock);
        }

        await SaveAsync(factory, competition, stage);
        return new Seed(competition.Id, stage.Id, group.Id, ids);
    }

    private async Task<Seed> SeedWithDraftDrawAsync(
        PlayUpWebApplicationFactory factory,
        int entryCount,
        int[] members)
    {
        var competition = CreateCompetition.Execute($"Membership-draft-{entryCount}", _clock);
        var ids = Enumerable.Range(0, entryCount)
            .Select(index => AddEntry.Execute(competition, $"D{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            competition,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        foreach (var index in members)
        {
            stage.AssignEntryToGroup(stage.Groups[0].Id, ids[index]);
        }

        // Domain create: the fixture only needs a draft; the launch rule is not the subject.
        _ = stage.CreateDraw(DrawResolutionKind.Group, _clock);

        await SaveAsync(factory, competition, stage);
        return new Seed(competition.Id, stage.Id, stage.Groups[0].Id, ids);
    }

    private async Task<Seed> SeedWithAppliedDrawAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("Membership-draw", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
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
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]);
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        await SaveAsync(factory, competition, stage);
        var group = stage.Groups[0];
        var members = group.EntryIds.ToArray();
        return new Seed(competition.Id, stage.Id, group.Id, members);
    }

    private static async Task SaveAsync(
        PlayUpWebApplicationFactory factory,
        Competition competition,
        Stage stage)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().Add(competition);
        scope.ServiceProvider.GetRequiredService<IStageRepository>().Add(stage);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    private sealed record Seed(CompetitionId CompetitionId, StageId StageId, GroupId GroupId, EntryId[] Entries);
}
