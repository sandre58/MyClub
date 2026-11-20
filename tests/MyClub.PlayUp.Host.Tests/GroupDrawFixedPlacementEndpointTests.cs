// -----------------------------------------------------------------------
// <copyright file="GroupDrawFixedPlacementEndpointTests.cs" company="Stéphane ANDRE">
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
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Groups draw with occupants kept as fixed placements, and the competition lock around create,
/// inputs, and generate.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class GroupDrawFixedPlacementEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Default_draw_keeps_legal_occupants_and_redo_keeps_themAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, occupyDifferentPots: true);
        using var client = factory.CreateClient();

        var drawId = await CreateDrawAsync(client, seed.StageId);
        using (var inputs = await client.PostAsJsonAsync(InputsUri(seed.StageId, drawId), new ConfigureDrawInputsRequest("Default")))
        {
            inputs.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generate = await client.PostAsync(GenerateUri(seed.StageId, drawId), content: null))
        {
            generate.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var apply = await client.PostAsync(PublishUri(seed.StageId, drawId), content: null))
        {
            apply.EnsureSuccessStatusCode();
        }

        var applied = await ReloadAsync(factory, seed.StageId);
        applied.Groups[0].EntryIds.Should().BeEquivalentTo(seed.Pinned);
        await AssertSqlInvariantsAsync(seed.StageId, applied: true);

        using var redo = await client.PostAsync(RedoUri(seed.StageId, drawId), content: null);
        redo.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterRedo = await ReloadAsync(factory, seed.StageId);
        afterRedo.Groups.SelectMany(group => group.EntryIds).Should().Contain(seed.Pinned);
        afterRedo.GetDraw(drawId).Status.Should().Be(DrawStatus.Cancelled);
        afterRedo.Draws.Count(draw => draw.Status != DrawStatus.Cancelled).Should().Be(1);
        await AssertSqlInvariantsAsync(seed.StageId, applied: false);
    }

    [IntegrationFact]
    public async Task Pot_conflict_and_pending_feed_refuse_create_without_a_draftAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var conflict = await SeedGroupsAsync(factory, occupySamePot: true);
        using var client = factory.CreateClient();

        using var refused = await client.PostAsJsonAsync(
            $"/stages/{conflict.StageId.Value}/draws",
            new CreateDrawRequest("Group"));
        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(refused)).Should().Be(ApplicationErrorCodes.DrawFixedPotConflict);
        (await ReloadAsync(factory, conflict.StageId)).Draws.Should().BeEmpty();

        var fed = await SeedGroupsAsync(factory, withPendingFeed: true);
        using var feed = await client.PostAsJsonAsync(
            $"/stages/{fed.StageId.Value}/draws",
            new CreateDrawRequest("Group"));
        feed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(feed)).Should().Be(ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed);
        (await ReloadAsync(factory, fed.StageId)).Draws.Should().BeEmpty();
        await AssertAtMostOneOpenDrawAsync(fed.StageId);
    }

    [IntegrationFact]
    public async Task Occupied_grid_with_rerun_refuses_create_without_a_draftAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, occupyDifferentPots: true);
        using var client = factory.CreateClient();

        using var refused = await client.PostAsJsonAsync(
            $"/stages/{seed.StageId.Value}/draws",
            new CreateDrawRequest("Group", "Rerun"));

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(refused)).Should().Be(ApplicationErrorCodes.DrawLaunchGridNotVacant);
        (await ReloadAsync(factory, seed.StageId)).Draws.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task Applied_groups_materialize_matchesAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, occupyDifferentPots: true);
        using var client = factory.CreateClient();
        var drawId = await CreateDrawAsync(client, seed.StageId);
        using (var inputs = await client.PostAsJsonAsync(InputsUri(seed.StageId, drawId), new ConfigureDrawInputsRequest("Default")))
        {
            inputs.EnsureSuccessStatusCode();
        }

        using (var generate = await client.PostAsync(GenerateUri(seed.StageId, drawId), content: null))
        {
            generate.EnsureSuccessStatusCode();
        }

        using (var apply = await client.PostAsync(PublishUri(seed.StageId, drawId), content: null))
        {
            apply.EnsureSuccessStatusCode();
        }

        var applied = await ReloadAsync(factory, seed.StageId);
        applied.Groups[0].EntryIds.Should().BeEquivalentTo(seed.Pinned);

        using (var prepare = await client.PostAsync($"/stages/{seed.StageId.Value}/prepare", content: null))
        {
            prepare.EnsureSuccessStatusCode();
        }

        using var materialize = await client.PostAsync($"/stages/{seed.StageId.Value}/matches/materialize", content: null);
        materialize.EnsureSuccessStatusCode();
        var body = await materialize.Content.ReadFromJsonAsync<MaterializeMatchesResponse>(HostJson.Options);
        body!.CreatedCount.Should().BeGreaterThan(0);
    }

    [IntegrationFact]
    public async Task Release_after_cancel_keeps_the_seeded_occupantsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, occupyDifferentPots: true);
        using var client = factory.CreateClient();
        var drawId = await CreateDrawAsync(client, seed.StageId);
        using (var inputs = await client.PostAsJsonAsync(InputsUri(seed.StageId, drawId), new ConfigureDrawInputsRequest("Default")))
        {
            inputs.EnsureSuccessStatusCode();
        }

        using (var generate = await client.PostAsync(GenerateUri(seed.StageId, drawId), content: null))
        {
            generate.EnsureSuccessStatusCode();
        }

        using (var apply = await client.PostAsync(PublishUri(seed.StageId, drawId), content: null))
        {
            apply.EnsureSuccessStatusCode();
        }

        using (var cancel = await client.PostAsync($"/stages/{seed.StageId.Value}/draws/{drawId.Value}/cancel", content: null))
        {
            cancel.EnsureSuccessStatusCode();
        }

        using var release = await client.PostAsync(
            $"/stages/{seed.StageId.Value}/draws/{drawId.Value}/release-aligned-placements",
            content: null);
        release.EnsureSuccessStatusCode();
        var after = await ReloadAsync(factory, seed.StageId);
        after.Groups.SelectMany(group => group.EntryIds).Should().BeEquivalentTo(seed.Pinned);
        await AssertOneEntryPerPhaseAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Create_racing_create_keeps_one_drawAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory);
        using var client = factory.CreateClient();

        var responses = await OverlapAsync(
            seed.CompetitionId,
            () => client.PostAsJsonAsync($"/stages/{seed.StageId.Value}/draws", new CreateDrawRequest("Group")),
            () => client.PostAsJsonAsync($"/stages/{seed.StageId.Value}/draws", new CreateDrawRequest("Group")));

        responses.Select(response => (int)response.StatusCode).Should().ContainSingle(code => code >= 200 && code < 300);
        var failure = responses.Single(response => (int)response.StatusCode >= 400);
        failure.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(failure)).Should().Be(StageErrorCodes.NonCancelledDrawAlreadyExists);
        (await ReloadAsync(factory, seed.StageId)).Draws.Count(draw => draw.Status != DrawStatus.Cancelled).Should().Be(1);
        await AssertAtMostOneOpenDrawAsync(seed.StageId);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Membership_racing_create_returns_one_conflictAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory);
        var loaded = await ReloadAsync(factory, seed.StageId);
        var samePot = GroupDrawPots.TryRead(loaded)!
            .GroupBy(item => item.Value)
            .First()
            .Select(item => item.Key.Value)
            .Take(2)
            .ToArray();
        using var client = factory.CreateClient();
        var responses = await OverlapAsync(
            seed.CompetitionId,
            () => client.PutAsJsonAsync(
                MembershipUri(seed),
                new SetGroupEntriesRequest(samePot, [])),
            () => client.PostAsJsonAsync($"/stages/{seed.StageId.Value}/draws", new CreateDrawRequest("Group")));

        responses.Count(response => (int)response.StatusCode is >= 200 and < 300).Should().Be(1);
        var failure = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        (await ReadCodeAsync(failure)).Should().BeOneOf(
            ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw,
            ApplicationErrorCodes.DrawFixedPotConflict);
        await AssertAtMostOneOpenDrawAsync(seed.StageId);
        await AssertOneEntryPerPhaseAsync(seed.StageId);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Generate_racing_generate_waits_and_keeps_one_resolutionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory);
        using var client = factory.CreateClient();
        var drawId = await CreateDrawAsync(client, seed.StageId);
        using (var inputs = await client.PostAsJsonAsync(InputsUri(seed.StageId, drawId), new ConfigureDrawInputsRequest("Default")))
        {
            inputs.EnsureSuccessStatusCode();
        }

        var responses = await OverlapAsync(
            seed.CompetitionId,
            () => client.PostAsync(GenerateUri(seed.StageId, drawId), content: null),
            () => client.PostAsync(GenerateUri(seed.StageId, drawId), content: null));

        responses.Should().OnlyContain(response => (int)response.StatusCode < 500);
        var stage = await ReloadAsync(factory, seed.StageId);
        stage.GetDraw(drawId).Resolution.State.Should().Be(DrawResolutionState.Resolved);
        await AssertAtMostOneOpenDrawAsync(seed.StageId);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Membership_racing_default_inputs_is_blocked_by_the_draftAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory);
        using var client = factory.CreateClient();
        var drawId = await CreateDrawAsync(client, seed.StageId);

        var responses = await OverlapAsync(
            seed.CompetitionId,
            () => client.PutAsJsonAsync(
                MembershipUri(seed),
                new SetGroupEntriesRequest([seed.Entries[0].Value], [])),
            () => client.PostAsJsonAsync(InputsUri(seed.StageId, drawId), new ConfigureDrawInputsRequest("Default")));

        responses.Should().ContainSingle(response => response.StatusCode == HttpStatusCode.OK);
        var failure = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        (await ReadCodeAsync(failure)).Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        (await ReloadAsync(factory, seed.StageId)).Groups.SelectMany(group => group.EntryIds).Should().BeEmpty();
        await AssertAtMostOneOpenDrawAsync(seed.StageId);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Redo_racing_membership_keeps_the_applied_placement_or_a_new_draftAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, occupyDifferentPots: true);
        using var client = factory.CreateClient();
        var drawId = await CreateDrawAsync(client, seed.StageId);
        using (var inputs = await client.PostAsJsonAsync(InputsUri(seed.StageId, drawId), new ConfigureDrawInputsRequest("Default")))
        {
            inputs.EnsureSuccessStatusCode();
        }

        using (var generate = await client.PostAsync(GenerateUri(seed.StageId, drawId), content: null))
        {
            generate.EnsureSuccessStatusCode();
        }

        using (var apply = await client.PostAsync(PublishUri(seed.StageId, drawId), content: null))
        {
            apply.EnsureSuccessStatusCode();
        }

        var applied = await ReloadAsync(factory, seed.StageId);
        var current = applied.Groups[0].EntryIds.Select(entry => entry.Value).ToArray();
        var responses = await OverlapAsync(
            seed.CompetitionId,
            () => client.PostAsync(RedoUri(seed.StageId, drawId), content: null),
            () => client.PutAsJsonAsync(
                MembershipUri(seed),
                new SetGroupEntriesRequest(current.Take(1).ToArray(), current)));

        responses.Should().ContainSingle(response => response.StatusCode == HttpStatusCode.OK);
        var failure = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        var code = await ReadCodeAsync(failure);
        code.Should().BeOneOf(
            ApplicationErrorCodes.AppliedDrawGroupPlacementProtected,
            ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        await AssertOneEntryPerPhaseAsync(seed.StageId);
        await AssertAtMostOneOpenDrawAsync(seed.StageId);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Inputs_racing_publish_waits_without_a_server_errorAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, occupyDifferentPots: true);
        using var client = factory.CreateClient();
        var drawId = await CreateDrawAsync(client, seed.StageId);
        using (var inputs = await client.PostAsJsonAsync(InputsUri(seed.StageId, drawId), new ConfigureDrawInputsRequest("Default")))
        {
            inputs.EnsureSuccessStatusCode();
        }

        using (var generate = await client.PostAsync(GenerateUri(seed.StageId, drawId), content: null))
        {
            generate.EnsureSuccessStatusCode();
        }

        var responses = await OverlapAsync(
            seed.CompetitionId,
            () => client.PostAsJsonAsync(InputsUri(seed.StageId, drawId), new ConfigureDrawInputsRequest("Default")),
            () => client.PostAsync(PublishUri(seed.StageId, drawId), content: null));

        responses.Should().OnlyContain(response => (int)response.StatusCode < 500);
        var stage = await ReloadAsync(factory, seed.StageId);
        stage.Draws.Count(draw => draw.Status != DrawStatus.Cancelled).Should().Be(1);
        await AssertOneEntryPerPhaseAsync(seed.StageId);
        await AssertAtMostOneOpenDrawAsync(seed.StageId);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    private async Task<Seed> SeedGroupsAsync(
        PlayUpWebApplicationFactory factory,
        bool occupyDifferentPots = false,
        bool occupySamePot = false,
        bool withPendingFeed = false)
    {
        var competition = CreateCompetition.Execute($"Groups-{Guid.NewGuid():N}", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            competition,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);

        Stage? source = null;
        if (withPendingFeed)
        {
            source = Stage.Create(competition.Id, new StageName("Source"), SampleRegulations.Standard(), _clock);
            source.ReplaceQualificationRules(
                new QualificationRules(
                [
                    new QualificationPath(
                        1,
                        QualificationSource.Overall(),
                        new QualificationSelection(SelectionMode.Position, 1),
                        QualificationDestination.ForGroup(stage.Id, stage.Groups[0].Id))
                ]),
                _clock);
            competition.AddStage(source.Id, _clock);
        }

        await SaveAsync(factory, competition, stage, source);
        EntryId[] pinned = [];
        if (occupySamePot)
        {
            pinned = await PlacePairAsync(factory, stage.Id, samePot: true);
        }
        else if (occupyDifferentPots)
        {
            pinned = await PlacePairAsync(factory, stage.Id, samePot: false);
        }

        return new Seed(competition.Id, stage.Id, stage.Groups[0].Id, ids, pinned);
    }

    private static async Task<EntryId[]> PlacePairAsync(PlayUpWebApplicationFactory factory, StageId stageId, bool samePot)
    {
        using var scope = factory.Services.CreateScope();
        var stage = (await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(stageId))!;
        var pots = GroupDrawPots.TryRead(stage)!;
        EntryId first;
        EntryId second;
        if (samePot)
        {
            var pair = pots.GroupBy(item => item.Value).First().Select(item => item.Key).Take(2).ToArray();
            first = pair[0];
            second = pair[1];
        }
        else
        {
            first = pots.First(item => item.Value == 1).Key;
            second = pots.First(item => item.Value == 2).Key;
        }

        stage.AssignEntryToGroup(stage.Groups[0].Id, first);
        stage.AssignEntryToGroup(stage.Groups[0].Id, second);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return [first, second];
    }

    private static async Task SaveAsync(
        PlayUpWebApplicationFactory factory,
        Competition competition,
        Stage stage,
        Stage? source)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().Add(competition);
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        if (source is not null)
        {
            stages.Add(source);
        }

        stages.Add(stage);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    private static async Task<DrawId> CreateDrawAsync(HttpClient client, StageId stageId)
    {
        using var create = await client.PostAsJsonAsync(
            $"/stages/{stageId.Value}/draws",
            new CreateDrawRequest("Group"));
        create.EnsureSuccessStatusCode();
        var summary = await create.Content.ReadFromJsonAsync<DrawSummaryDto>(HostJson.Options);
        return new DrawId(summary!.DrawId);
    }

    private async Task AssertSqlInvariantsAsync(StageId stageId, bool applied)
    {
        await AssertOneEntryPerPhaseAsync(stageId);
        await AssertAtMostOneOpenDrawAsync(stageId);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT inputs::text, resolution::text
            FROM draws
            WHERE stage_id = @stage AND status <> 2
            """,
            connection);
        command.Parameters.AddWithValue("stage", stageId.Value);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = 0;
        while (await reader.ReadAsync())
        {
            rows++;
            using var inputs = JsonDocument.Parse(reader.GetString(0));
            using var resolution = JsonDocument.Parse(reader.GetString(1));
            var pots = inputs.RootElement.GetProperty("Pots").EnumerateArray()
                .ToDictionary(
                    item => item.GetProperty("EntryId").GetGuid(),
                    item => item.GetProperty("Value").GetInt32());
            var fixedIds = inputs.RootElement.TryGetProperty("FixedGroups", out var fixedGroups)
                ? fixedGroups.EnumerateArray().Select(item => item.GetProperty("EntryId").GetGuid()).ToHashSet()
                : [];
            var placed = resolution.RootElement.GetProperty("GroupResults").EnumerateArray()
                .Select(item => (
                    Entry: item.GetProperty("EntryId").GetGuid(),
                    Group: item.GetProperty("GroupId").GetGuid()))
                .ToArray();
            fixedIds.Should().BeSubsetOf(placed.Select(item => item.Entry));
            placed.GroupBy(item => item.Group).ToList().ForEach(group =>
            {
                group.Select(item => pots[item.Entry]).Should().OnlyHaveUniqueItems();
            });
            if (applied)
            {
                var occupancy = await ReadOccupancyAsync(stageId);
                occupancy.Should().BeEquivalentTo(placed);
            }
        }

        rows.Should().Be(1);
    }

    private async Task AssertOneEntryPerPhaseAsync(StageId stageId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)::int FROM (
                SELECT ge.entry_id
                FROM group_entries ge
                JOIN groups g ON g.id = ge.group_id
                WHERE g.stage_id = @stage
                GROUP BY ge.entry_id
                HAVING COUNT(*) > 1) duplicated
            """,
            connection);
        command.Parameters.AddWithValue("stage", stageId.Value);
        var duplicated = (int)(await command.ExecuteScalarAsync() ?? 0);
        duplicated.Should().Be(0);
    }

    private async Task AssertAtMostOneOpenDrawAsync(StageId stageId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*)::int FROM draws WHERE stage_id = @stage AND status <> 2",
            connection);
        command.Parameters.AddWithValue("stage", stageId.Value);
        var open = (int)(await command.ExecuteScalarAsync() ?? 0);
        open.Should().BeLessThanOrEqualTo(1);
    }

    private async Task<List<(Guid Entry, Guid Group)>> ReadOccupancyAsync(StageId stageId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT ge.entry_id, ge.group_id
            FROM group_entries ge
            JOIN groups g ON g.id = ge.group_id
            WHERE g.stage_id = @stage
            """,
            connection);
        command.Parameters.AddWithValue("stage", stageId.Value);
        var rows = new List<(Guid Entry, Guid Group)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add((reader.GetGuid(0), reader.GetGuid(1)));
        }

        return rows;
    }

    private async Task<HttpResponseMessage[]> OverlapAsync(
        CompetitionId competitionId,
        Func<Task<HttpResponseMessage>> left,
        Func<Task<HttpResponseMessage>> right)
    {
        var key = CompetitionAdvisoryKey(competitionId);
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

    private static long CompetitionAdvisoryKey(CompetitionId competitionId)
    {
        var bytes = competitionId.Value.ToByteArray();
        return BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
    }

    private static Uri MembershipUri(Seed seed) =>
        new($"/stages/{seed.StageId.Value}/groups/{seed.GroupId.Value}/membership", UriKind.Relative);

    private static string InputsUri(StageId stageId, DrawId drawId) =>
        $"/stages/{stageId.Value}/draws/{drawId.Value}/inputs";

    private static Uri GenerateUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/generate", UriKind.Relative);

    private static Uri PublishUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/publish-and-apply", UriKind.Relative);

    private static Uri RedoUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/redo", UriKind.Relative);

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
        return (await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(stageId))!;
    }

    private sealed record Seed(
        CompetitionId CompetitionId,
        StageId StageId,
        GroupId GroupId,
        EntryId[] Entries,
        EntryId[] Pinned);
}
