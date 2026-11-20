// -----------------------------------------------------------------------
// <copyright file="GroupReplaceBatchEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class GroupReplaceBatchEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Replace_returns_204_and_keeps_the_seat_after_reloadAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 4, entryCount: 4, members: [0, 1, 2]);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            ReplaceUri(seed, seed.Entries[1]),
            new { replacementEntryId = seed.Entries[3].Value });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reloaded = await ReloadAsync(factory, seed.StageId);
        reloaded.GetGroup(seed.GroupId).EntryIds.Should().Equal(seed.Entries[0], seed.Entries[3], seed.Entries[2]);
    }

    [IntegrationFact]
    public async Task Replace_replay_returns_204_twiceAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 3, members: [0]);
        using var client = factory.CreateClient();
        var body = new { replacementEntryId = seed.Entries[1].Value };

        using var first = await client.PostAsJsonAsync(ReplaceUri(seed, seed.Entries[0]), body);
        using var second = await client.PostAsJsonAsync(ReplaceUri(seed, seed.Entries[0]), body);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reloaded = await ReloadAsync(factory, seed.StageId);
        reloaded.GetGroup(seed.GroupId).EntryIds.Should().Equal(seed.Entries[1]);
    }

    [IntegrationFact]
    public async Task Replace_returns_400_for_a_missing_replacement_and_404_for_an_unknown_stageAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 2, members: [0]);
        using var client = factory.CreateClient();

        using var missing = await client.PostAsJsonAsync(ReplaceUri(seed, seed.Entries[0]), new { });
        using var empty = await client.PostAsJsonAsync(
            ReplaceUri(seed, seed.Entries[0]),
            new { replacementEntryId = Guid.Empty });
        using var unknown = await client.PostAsJsonAsync(
            $"/stages/{Guid.NewGuid()}/groups/{seed.GroupId.Value}/entries/{seed.Entries[0].Value}/replace",
            new { replacementEntryId = seed.Entries[1].Value });

        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadCodeAsync(missing)).Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadCodeAsync(empty)).Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadCodeAsync(unknown)).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Replace_returns_409_for_each_refusal_familyAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 4, members: [0]);
        await PlaceAsync(factory, seed.StageId, seed.OtherGroupId, seed.Entries[2]);
        using var client = factory.CreateClient();

        using var absent = await client.PostAsJsonAsync(
            ReplaceUri(seed, seed.Entries[1]),
            new { replacementEntryId = seed.Entries[3].Value });
        using var duplicate = await client.PostAsJsonAsync(
            ReplaceUri(seed, seed.Entries[0]),
            new { replacementEntryId = seed.Entries[2].Value });

        absent.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(absent)).Should().Be(StageErrorCodes.EntryNotFound);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(duplicate)).Should().Be(StageErrorCodes.DuplicateEntry);

        var matched = await SeedWithMatchAsync(factory);
        using var locked = await client.PostAsJsonAsync(
            ReplaceUri(matched, matched.Entries[0]),
            new { replacementEntryId = matched.Entries[2].Value });
        locked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(locked)).Should().Be(StageErrorCodes.GroupClearLockedByStageMatches);

        var drafted = await SeedWithDraftDrawAsync(factory);
        using var draw = await client.PostAsJsonAsync(
            ReplaceUri(drafted, drafted.Entries[0]),
            new { replacementEntryId = drafted.Entries[1].Value });
        draw.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(draw)).Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
    }

    [IntegrationFact]
    public async Task Batch_returns_204_and_400_for_an_invalid_listAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 4, entryCount: 3, members: [0]);
        using var client = factory.CreateClient();

        using var placed = await client.PostAsJsonAsync(
            BatchUri(seed),
            new { entryIds = new[] { seed.Entries[2].Value, seed.Entries[0].Value, seed.Entries[1].Value } });
        using var empty = await client.PostAsJsonAsync(BatchUri(seed), new { entryIds = Array.Empty<Guid>() });
        using var duplicate = await client.PostAsJsonAsync(
            BatchUri(seed),
            new { entryIds = new[] { seed.Entries[1].Value, seed.Entries[1].Value } });
        using var tooMany = await client.PostAsJsonAsync(
            BatchUri(seed),
            new { entryIds = Enumerable.Range(0, 65).Select(_ => Guid.NewGuid()).ToArray() });

        placed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reloaded = await ReloadAsync(factory, seed.StageId);
        reloaded.GetGroup(seed.GroupId).EntryIds.Should().Equal(seed.Entries[0], seed.Entries[2], seed.Entries[1]);
        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadCodeAsync(empty)).Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        tooMany.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadCodeAsync(tooMany)).Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
    }

    [IntegrationFact]
    public async Task Batch_refusal_returns_409_with_entry_id_and_writes_nothingAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 3, members: []);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            BatchUri(seed),
            new { entryIds = seed.Entries.Select(id => id.Value).ToArray() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ReadProblemAsync(response);
        problem.Code.Should().Be(StageErrorCodes.GroupFull);
        problem.EntryId.Should().Be(seed.Entries[2].Value);
        var reloaded = await ReloadAsync(factory, seed.StageId);
        reloaded.GetGroup(seed.GroupId).EntryIds.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task Two_concurrent_batches_fill_the_group_onceAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 4, members: []);
        using var client = factory.CreateClient();
        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => client.PostAsJsonAsync(
                BatchUri(seed),
                new { entryIds = new[] { seed.Entries[0].Value, seed.Entries[1].Value } }),
            () => client.PostAsJsonAsync(
                BatchUri(seed),
                new { entryIds = new[] { seed.Entries[2].Value, seed.Entries[3].Value } }));

        responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
            [HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
        var refused = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        (await ReadCodeAsync(refused)).Should().Be(StageErrorCodes.GroupFull);
        responses.Should().OnlyContain(response => (int)response.StatusCode < 500);
        var reloaded = await ReloadAsync(factory, seed.StageId);
        reloaded.GetGroup(seed.GroupId).EntryIds.Should().HaveCount(2);
        AssertOneGroupEach(reloaded);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Batch_parallel_with_delete_conflicts_on_group_membershipAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 3, members: [0]);
        using var client = factory.CreateClient();
        var deleted = seed.Entries[0];

        var raced = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => client.PostAsJsonAsync(
                BatchUri(seed),
                new { entryIds = new[] { seed.Entries[1].Value, seed.Entries[2].Value } }),
            () => client.PostAsync(DeleteUri(seed.CompetitionId, deleted), null));

        raced.Should().OnlyContain(response => (int)response.StatusCode < 500);
        raced[1].StatusCode.Should().Be(HttpStatusCode.OK);
        raced[0].StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.Conflict);
        var afterRace = await ReloadAsync(factory, seed.StageId);
        AssertOneGroupEach(afterRace);
        afterRace.Groups.SelectMany(group => group.EntryIds).Should().NotContain(deleted);
        if (raced[0].StatusCode == HttpStatusCode.NoContent)
        {
            afterRace.GetGroup(seed.GroupId).EntryIds.Should().Equal(seed.Entries[1], seed.Entries[2]);
        }
        else
        {
            (await ReadCodeAsync(raced[0])).Should().Be(StageErrorCodes.GroupFull);
            afterRace.GetGroup(seed.GroupId).EntryIds.Should().BeEmpty();
        }

        foreach (var response in raced)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Two_concurrent_puts_on_the_last_seat_fill_the_group_onceAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var lastSeat = await SeedAsync(factory, places: 2, entryCount: 3, members: [0]);
        using var client = factory.CreateClient();
        var puts = await OverlapOnCompetitionLockAsync(
            lastSeat.CompetitionId,
            () => client.PutAsync(EntryUri(lastSeat, lastSeat.Entries[1]), null),
            () => client.PutAsync(EntryUri(lastSeat, lastSeat.Entries[2]), null));

        puts.Select(response => response.StatusCode).Should().BeEquivalentTo(
            [HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
        puts.Should().OnlyContain(response => (int)response.StatusCode < 500);
        (await ReadCodeAsync(puts.Single(response => response.StatusCode == HttpStatusCode.Conflict)))
            .Should().Be(StageErrorCodes.GroupFull);
        var reloaded = await ReloadAsync(factory, lastSeat.StageId);
        reloaded.GetGroup(lastSeat.GroupId).EntryIds.Should().HaveCount(2);
        AssertOneGroupEach(reloaded);
        foreach (var response in puts)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Two_concurrent_puts_of_one_entry_into_two_groups_keep_a_single_membershipAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 1, members: []);
        using var client = factory.CreateClient();
        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => client.PutAsync(EntryUri(seed, seed.Entries[0]), null),
            () => client.PutAsync(
                new Uri(
                    $"/stages/{seed.StageId.Value}/groups/{seed.OtherGroupId.Value}/entries/{seed.Entries[0].Value}",
                    UriKind.Relative),
                null));

        responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
            [HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
        responses.Should().OnlyContain(response => (int)response.StatusCode < 500);
        (await ReadCodeAsync(responses.Single(response => response.StatusCode == HttpStatusCode.Conflict)))
            .Should().Be(StageErrorCodes.DuplicateEntry);
        var reloaded = await ReloadAsync(factory, seed.StageId);
        reloaded.Groups.SelectMany(group => group.EntryIds).Should().Equal(seed.Entries[0]);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [IntegrationFact]
    public async Task Batch_unknown_group_returns_409_without_entryIdAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAsync(factory, places: 2, entryCount: 1, members: []);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/stages/{seed.StageId.Value}/groups/{Guid.NewGuid()}/entries",
            new { entryIds = new[] { seed.Entries[0].Value } });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ReadProblemAsync(response);
        problem.Code.Should().Be(StageErrorCodes.GroupNotFound);
        problem.EntryId.Should().BeNull();
    }

    private static void AssertOneGroupEach(Stage stage)
    {
        var seen = new HashSet<Guid>();
        foreach (var entryId in stage.Groups.SelectMany(group => group.EntryIds))
        {
            seen.Add(entryId.Value).Should().BeTrue();
        }
    }

    private static Uri ReplaceUri(Seed seed, EntryId occupant) =>
        new(
            $"/stages/{seed.StageId.Value}/groups/{seed.GroupId.Value}/entries/{occupant.Value}/replace",
            UriKind.Relative);

    private static Uri BatchUri(Seed seed) =>
        new($"/stages/{seed.StageId.Value}/groups/{seed.GroupId.Value}/entries", UriKind.Relative);

    private static Uri EntryUri(Seed seed, EntryId entryId) =>
        new(
            $"/stages/{seed.StageId.Value}/groups/{seed.GroupId.Value}/entries/{entryId.Value}",
            UriKind.Relative);

    private static Uri DeleteUri(CompetitionId competitionId, EntryId entryId) =>
        new($"/competitions/{competitionId.Value}/entries/{entryId.Value}/delete", UriKind.Relative);

    /// <summary>
    /// Holds the competition advisory lock, starts both writes, and releases the lock only once both are waiting on it.
    /// </summary>
    private async Task<HttpResponseMessage[]> OverlapOnCompetitionLockAsync(
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

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response) =>
        (await ReadProblemAsync(response)).Code;

    private static async Task<(string? Code, Guid? EntryId)> ReadProblemAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        return (ReadExtension(problem, "code"), Guid.TryParse(ReadExtension(problem, "entryId"), out var entryId) ? entryId : null);
    }

    private static string? ReadExtension(ProblemDetails? problem, string name)
    {
        if (problem is null || !problem.Extensions.TryGetValue(name, out var raw) || raw is null)
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

    private async Task<Seed> SeedAsync(
        PlayUpWebApplicationFactory factory,
        int places,
        int entryCount,
        int[] members,
        bool running = false)
    {
        var competition = CreateCompetition.Execute($"G-http-{places}-{entryCount}-{members.Length}", _clock);
        var ids = Enumerable.Range(0, entryCount)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, places), _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        foreach (var index in members)
        {
            stage.AssignEntryToGroup(stage.Groups[0].Id, ids[index]);
        }

        if (running)
        {
            competition.Prepare(_clock);
            competition.Start(_clock);
        }

        await SaveAsync(factory, competition, stage);
        return new Seed(competition.Id, stage.Id, stage.Groups[0].Id, stage.Groups[1].Id, ids);
    }

    private async Task<Seed> SeedWithMatchAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("G-http-replace-match", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"M{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 4), _clock).Stage;
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[2]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, ids[3]);
        var matchday = stage.Matchdays.Count == 0 ? stage.AddMatchday(1, _clock) : stage.Matchdays[0];
        var addFixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, ids[2], ids[3], _clock);
        stage.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
        stage.Prepare(_clock);

        await SaveAsync(factory, competition, stage, match);
        return new Seed(competition.Id, stage.Id, stage.Groups[0].Id, stage.Groups[1].Id, ids);
    }

    private async Task<Seed> SeedWithDraftDrawAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("G-http-replace-draft", _clock);
        var ids = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"D{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            competition,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        _ = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);

        await SaveAsync(factory, competition, stage);
        return new Seed(competition.Id, stage.Id, stage.Groups[0].Id, stage.Groups[1].Id, ids);
    }

    private static async Task PlaceAsync(
        PlayUpWebApplicationFactory factory,
        StageId stageId,
        GroupId groupId,
        EntryId entryId)
    {
        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var stage = await stages.GetByIdForUpdateAsync(stageId) ?? throw new InvalidOperationException("Stage missing.");
        stage.AssignEntryToGroup(groupId, entryId);
        await unitOfWork.SaveChangesAsync();
    }

    private static async Task SaveAsync(
        PlayUpWebApplicationFactory factory,
        Competition competition,
        Stage stage,
        Match? match = null)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().Add(competition);
        scope.ServiceProvider.GetRequiredService<IStageRepository>().Add(stage);
        if (match is not null)
        {
            scope.ServiceProvider.GetRequiredService<IMatchRepository>().Add(match);
        }

        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    private static async Task<Stage> ReloadAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        return await stages.GetByIdForUpdateAsync(stageId) ?? throw new InvalidOperationException("Stage missing.");
    }

    private sealed record Seed(
        CompetitionId CompetitionId,
        StageId StageId,
        GroupId GroupId,
        GroupId OtherGroupId,
        EntryId[] Entries);
}
