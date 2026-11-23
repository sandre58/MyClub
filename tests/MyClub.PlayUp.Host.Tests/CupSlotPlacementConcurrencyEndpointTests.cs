// -----------------------------------------------------------------------
// <copyright file="CupSlotPlacementConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Postgres races for Cup slot placement, match production, and the <c>expectedEntryId</c> contract.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CupSlotPlacementConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Parallel_puts_of_the_same_team_on_two_slots_yield_one_duplicateAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedOpenCupAsync(factory, entryCount: 2);
        using var client = factory.CreateClient();

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => PutSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[0], expected: null),
            () => PutSlotAsync(client, seed.StageId, seed.OpenSlots[1], seed.Entries[0], expected: null));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().ContainSingle(outcome => outcome.Status == HttpStatusCode.NoContent);
        outcomes.Should().ContainSingle(outcome =>
            outcome.Status == HttpStatusCode.Conflict && outcome.Code == StageErrorCodes.DuplicateEntry);
        await AssertCupSqlAsync(seed.StageId);
        (await CountOccupantsAsync(seed.StageId, seed.Entries[0])).Should().Be(1);
    }

    [IntegrationFact]
    public async Task Parallel_puts_on_the_same_free_slot_yield_one_staleAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedOpenCupAsync(factory, entryCount: 2);
        using var client = factory.CreateClient();

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => PutSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[0], expected: null),
            () => PutSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[1], expected: null));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().ContainSingle(outcome => outcome.Status == HttpStatusCode.NoContent);
        outcomes.Should().ContainSingle(outcome =>
            outcome.Status == HttpStatusCode.Conflict
            && outcome.Code == ApplicationErrorCodes.SlotAssignmentStale);
        await AssertCupSqlAsync(seed.StageId);
        (await ReadOccupantAsync(seed.StageId, seed.OpenSlots[0])).Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task Parallel_deletes_of_the_same_occupant_yield_one_staleAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedOpenCupAsync(factory, entryCount: 1);
        using var client = factory.CreateClient();
        (await PutSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[0], expected: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => DeleteSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[0]),
            () => DeleteSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[0]));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().ContainSingle(outcome => outcome.Status == HttpStatusCode.NoContent);
        outcomes.Should().ContainSingle(outcome =>
            outcome.Status == HttpStatusCode.Conflict
            && outcome.Code == ApplicationErrorCodes.SlotAssignmentStale);
        (await ReadOccupantAsync(seed.StageId, seed.OpenSlots[0])).Should().BeNull();
        await AssertCupSqlAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Parallel_put_and_delete_on_the_same_slot_yield_one_staleAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedOpenCupAsync(factory, entryCount: 2);
        using var client = factory.CreateClient();
        (await PutSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[0], expected: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => PutSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[1], seed.Entries[0]),
            () => DeleteSlotAsync(client, seed.StageId, seed.OpenSlots[0], seed.Entries[0]));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().ContainSingle(outcome => outcome.Status == HttpStatusCode.NoContent);
        outcomes.Should().ContainSingle(outcome =>
            outcome.Status == HttpStatusCode.Conflict
            && outcome.Code == ApplicationErrorCodes.SlotAssignmentStale);
        var occupant = await ReadOccupantAsync(seed.StageId, seed.OpenSlots[0]);
        (occupant is null || occupant == seed.Entries[1]).Should().BeTrue();
        await AssertCupSqlAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Parallel_put_and_cup_generation_stay_coherentAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedReadyCupAsync(factory, entryCount: 5, occupy: 4);
        using var client = factory.CreateClient();
        var replaced = seed.OpenSlots[0];
        var previous = await ReadOccupantAsync(seed.StageId, replaced);

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => PutSlotAsync(client, seed.StageId, replaced, seed.Entries[4], previous),
            () => PostCupGenerationAsync(client, seed.StageId));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().ContainSingle(outcome => outcome.Status == HttpStatusCode.OK);
        var placement = outcomes.Single(outcome => outcome.Status != HttpStatusCode.OK);
        if (placement.Status != HttpStatusCode.NoContent)
        {
            // Generation first attaches matches. A complete cup then refuses the
            // placement with StructureNotMutable (HTTP 400). A divergent occupant
            // on a still-editable cup is a 409 coherence code.
            placement.Code.Should().BeOneOf(
                ApplicationErrorCodes.StructureNotMutable,
                ApplicationErrorCodes.SlotAssignmentStale,
                StageErrorCodes.MaterializedOpponentConflict,
                StageErrorCodes.DuplicateEntry);
        }

        outcomes.Should().OnlyContain(outcome => (int)outcome.Status < 500);
        await AssertCupSqlAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Parallel_cup_generations_create_one_match_setAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedReadyCupAsync(factory, entryCount: 4, occupy: 4);
        using var client = factory.CreateClient();

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => PostCupGenerationAsync(client, seed.StageId),
            () => PostCupGenerationAsync(client, seed.StageId));
        var bodies = await ReadMaterializeAsync(responses);

        bodies.Should().OnlyContain(body => body.Status == HttpStatusCode.OK);
        bodies.Count(body => body is { CreatedCount: > 0, AlreadyComplete: false }).Should().Be(1);
        bodies.Count(body => body.AlreadyComplete).Should().Be(1);
        (await CountMatchesAsync(seed.StageId)).Should().Be(bodies.Sum(body => body.CreatedCount));
        await AssertCupSqlAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Parallel_dematerialize_and_put_do_not_500Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedMaterializedCupAsync(factory);
        using var client = factory.CreateClient();
        var slot = seed.OpenSlots[0];
        var previous = await ReadOccupantAsync(seed.StageId, slot);

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => PostDematerializeAsync(client, seed.StageId),
            () => PutSlotAsync(client, seed.StageId, slot, seed.Entries[4], previous));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().OnlyContain(outcome => (int)outcome.Status < 500);
        await AssertCupSqlAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Parallel_dematerialize_and_generation_do_not_500Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedMaterializedCupAsync(factory);
        using var client = factory.CreateClient();

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => PostDematerializeAsync(client, seed.StageId),
            () => PostCupGenerationAsync(client, seed.StageId));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().OnlyContain(outcome => outcome.Status == HttpStatusCode.OK);
        await AssertCupSqlAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Parallel_apply_draw_and_put_respect_draw_protectionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedPublishedSlotDrawAsync(factory);
        using var client = factory.CreateClient();

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => client.PostAsync(ApplyDrawPath(seed.StageId, seed.DrawId!.Value.Value), content: null),
            () => PutSlotAsync(client, seed.StageId, seed.TargetSlot!, seed.ConflictingEntry!.Value.Value, expected: null));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().OnlyContain(outcome => (int)outcome.Status < 500);
        var stage = await ReloadStageAsync(factory, seed.StageId);
        var draw = stage.GetDraw(seed.DrawId!.Value);
        if (DrawAppliedState.IsApplied(draw, stage))
        {
            foreach (var placed in draw.Resolution.SlotResults)
            {
                stage.FindSlot(placed.SlotKey)!.EntryId.Should().Be(placed.EntryId);
            }
        }
        else
        {
            stage.FindSlot(seed.TargetSlot)!.EntryId.Should().Be(seed.ConflictingEntry);
        }

        await AssertCupSqlAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Parallel_membership_and_group_materialize_follow_the_validated_rosterAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedReadyGroupsAsync(factory);
        using var client = factory.CreateClient();
        var next = new[] { seed.Entries[0], seed.Entries[1], seed.Entries[4] };

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => client.PutAsJsonAsync(
                MembershipPath(seed.StageId, seed.GroupId),
                new SetGroupEntriesRequest(next, [seed.Entries[0], seed.Entries[1]]),
                HostJson.Options),
            () => client.PostAsync(MaterializePath(seed.StageId), content: null));
        var outcomes = await ReadAsync(responses);

        outcomes.Should().Contain(outcome => outcome.Status == HttpStatusCode.OK);
        outcomes.Should().OnlyContain(outcome => (int)outcome.Status < 500);
        await AssertGroupMatchesFollowMembershipAsync(factory, seed.StageId);
    }

    [IntegrationFact]
    public async Task Parallel_group_materialize_creates_one_match_setAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedReadyGroupsAsync(factory);
        using var client = factory.CreateClient();

        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => client.PostAsync(MaterializePath(seed.StageId), content: null),
            () => client.PostAsync(MaterializePath(seed.StageId), content: null));
        var bodies = await ReadMaterializeAsync(responses);

        bodies.Should().OnlyContain(body => body.Status == HttpStatusCode.OK);
        bodies.Count(body => body is { CreatedCount: > 0, AlreadyComplete: false }).Should().Be(1);
        bodies.Count(body => body.AlreadyComplete).Should().Be(1);
        await AssertGroupMatchesFollowMembershipAsync(factory, seed.StageId);
    }

    [IntegrationFact]
    public async Task Nominal_slot_assignment_contractAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedOpenCupAsync(factory, entryCount: 2);
        using var client = factory.CreateClient();
        var slot = seed.OpenSlots[0];

        using (var missing = new StringContent(
            $$"""{"entryId":"{{seed.Entries[0]}}"}""",
            Encoding.UTF8,
            "application/json"))
        {
            using var response = await client.PutAsync(AssignmentPath(seed.StageId, slot), missing);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadCodeAsync(response)).Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
        }

        (await PutSlotAsync(client, seed.StageId, slot, seed.Entries[0], expected: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadOccupantAsync(seed.StageId, slot)).Should().Be(seed.Entries[0]);

        (await PutSlotAsync(client, seed.StageId, slot, seed.Entries[1], expected: null))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadOccupantAsync(seed.StageId, slot)).Should().Be(seed.Entries[0]);

        (await PutSlotAsync(client, seed.StageId, slot, seed.Entries[0], seed.Entries[0]))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadOccupantAsync(seed.StageId, slot)).Should().Be(seed.Entries[0]);

        (await DeleteSlotAsync(client, seed.StageId, slot, Guid.NewGuid()))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadOccupantAsync(seed.StageId, slot)).Should().Be(seed.Entries[0]);

        (await DeleteSlotAsync(client, seed.StageId, slot, seed.Entries[0]))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadOccupantAsync(seed.StageId, slot)).Should().BeNull();

        (await DeleteSlotAsync(client, seed.StageId, slot, expected: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadOccupantAsync(seed.StageId, slot)).Should().BeNull();
        await AssertCupSqlAsync(seed.StageId);
    }

    [IntegrationFact]
    public async Task Nominal_cup_generation_and_dematerializeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedReadyCupAsync(factory, entryCount: 4, occupy: 4);
        using var client = factory.CreateClient();

        using (var created = await PostCupGenerationAsync(client, seed.StageId))
        {
            created.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await CountMatchesAsync(seed.StageId)).Should().BeGreaterThan(0);
        await AssertCupSqlAsync(seed.StageId);

        using var removed = await PostDematerializeAsync(client, seed.StageId);
        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await CountMatchesAsync(seed.StageId)).Should().Be(0);
    }

    [IntegrationFact]
    public async Task Nominal_group_materializeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedReadyGroupsAsync(factory);
        using var client = factory.CreateClient();

        using var created = await client.PostAsync(MaterializePath(seed.StageId), content: null);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertGroupMatchesFollowMembershipAsync(factory, seed.StageId);

        using var removed = await PostDematerializeAsync(client, seed.StageId);
        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await CountMatchesAsync(seed.StageId)).Should().Be(0);
    }

    private async Task<CupSeed> SeedOpenCupAsync(PlayUpWebApplicationFactory factory, int entryCount)
    {
        var (competition, stage, entries) = BuildCup(entryCount);
        var open = ProbeOpenSlots(stage, entries[0]);
        open.Length.Should().BeGreaterThanOrEqualTo(2);
        await SaveAsync(factory, competition, stage, matches: null);
        return new CupSeed(competition.Id, stage.Id, entries, open, DrawId: null, TargetSlot: null, ConflictingEntry: null);
    }

    private async Task<CupSeed> SeedReadyCupAsync(PlayUpWebApplicationFactory factory, int entryCount, int occupy)
    {
        var (competition, stage, entries) = BuildCup(entryCount);
        var occupied = AssignOpenSlots(stage, [.. entries.Take(occupy)]);
        stage.Prepare(_clock);
        await SaveAsync(factory, competition, stage, matches: null);
        return new CupSeed(competition.Id, stage.Id, entries, occupied, DrawId: null, TargetSlot: null, ConflictingEntry: null);
    }

    private async Task<CupSeed> SeedMaterializedCupAsync(PlayUpWebApplicationFactory factory)
    {
        var (competition, stage, entries) = BuildCup(entryCount: 5);
        var occupied = AssignOpenSlots(stage, [.. entries.Take(4)]);
        stage.Prepare(_clock);
        var created = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            pairKeys: null,
            existingMatches: [],
            _clock,
            competitionStages: [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>(),
            planEntireRound: true).CreatedMatches;
        created.Should().NotBeEmpty();
        await SaveAsync(factory, competition, stage, created);
        return new CupSeed(competition.Id, stage.Id, entries, occupied, DrawId: null, TargetSlot: null, ConflictingEntry: null);
    }

    private async Task<CupSeed> SeedPublishedSlotDrawAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("A10-draw", _clock);
        for (var index = 0; index < 4; index++)
        {
            AddEntry.Execute(competition, $"D{index}", _clock);
        }

        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        var entries = competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ReplaceAffectationAuthoring(entries, _clock);
        ReplaceStageDrawRules.Execute(stage, competition, new DrawRules(DrawMode.Random), _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        var slotTargets = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets).IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        var placed = draw.Resolution.SlotResults[0];
        var conflicting = entries.First(id => !id.Equals(placed.EntryId));
        await SaveAsync(factory, competition, stage, matches: null);
        return new CupSeed(
            competition.Id,
            stage.Id,
            [.. entries.Select(id => id.Value)],
            [],
            draw.Id,
            placed.SlotKey,
            conflicting);
    }

    private async Task<GroupSeed> SeedReadyGroupsAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("A10-groups", _clock);
        var entries = Enumerable.Range(0, 5)
            .Select(index => AddEntry.Execute(competition, $"G{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 3), _clock).Stage;
        stage.ReplaceAffectationAuthoring(entries, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, entries[0]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, entries[1]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, entries[2]);
        stage.AssignEntryToGroup(stage.Groups[1].Id, entries[3]);
        stage.Prepare(_clock);
        await SaveAsync(factory, competition, stage, matches: null);
        return new GroupSeed(
            competition.Id,
            stage.Id,
            stage.Groups[0].Id,
            [.. entries.Select(id => id.Value)]);
    }

    private (Competition Competition, Stage Stage, Guid[] Entries) BuildCup(int entryCount)
    {
        var competition = CreateCompetition.Execute("A10-cup", _clock);
        for (var index = 0; index < entryCount; index++)
        {
            AddEntry.Execute(competition, $"C{index}", _clock);
        }

        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;
        var entries = competition.Entries.Select(entry => entry.Id.Value).ToArray();
        stage.ReplaceAffectationAuthoring([.. competition.Entries.Select(entry => entry.Id)], _clock);
        return (competition, stage, entries);
    }

    private static string[] ProbeOpenSlots(Stage stage, Guid probe)
    {
        var entryId = new EntryId(probe);
        var keys = new List<string>();
        foreach (var slot in stage.Slots)
        {
            try
            {
                stage.AssignEntryToSlot(slot.SlotKey, entryId);
            }
            catch (DomainException exception) when (exception.Code == StageErrorCodes.SlotFeedConflict)
            {
                continue;
            }

            keys.Add(slot.SlotKey);
            stage.ClearSlotAssignment(slot.SlotKey);
        }

        return [.. keys];
    }

    private static string[] AssignOpenSlots(Stage stage, Guid[] entries)
    {
        var keys = new List<string>();
        foreach (var slot in stage.Slots)
        {
            if (keys.Count >= entries.Length)
            {
                break;
            }

            try
            {
                stage.AssignEntryToSlot(slot.SlotKey, new EntryId(entries[keys.Count]));
            }
            catch (DomainException exception) when (exception.Code == StageErrorCodes.SlotFeedConflict)
            {
                continue;
            }

            keys.Add(slot.SlotKey);
        }

        keys.Count.Should().Be(entries.Length);
        return [.. keys];
    }

    private static async Task SaveAsync(
        PlayUpWebApplicationFactory factory,
        Competition competition,
        Stage stage,
        IReadOnlyList<Match>? matches)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        stages.Add(stage);
        if (matches is not null)
        {
            var matchRepository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            foreach (var match in matches)
            {
                matchRepository.Add(match);
            }
        }

        await unitOfWork.SaveChangesAsync();
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

    private static Task<HttpResponseMessage> PutSlotAsync(
        HttpClient client,
        StageId stageId,
        string slotKey,
        Guid entryId,
        Guid? expected)
    {
        var expectedJson = expected is null ? "null" : $"\"{expected}\"";
        var json = $$"""{"entryId":"{{entryId}}","expectedEntryId":{{expectedJson}}}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return client.PutAsync(AssignmentPath(stageId, slotKey), content);
    }

    private static Task<HttpResponseMessage> DeleteSlotAsync(
        HttpClient client,
        StageId stageId,
        string slotKey,
        Guid? expected)
    {
        var path = expected is null
            ? AssignmentPath(stageId, slotKey)
            : $"{AssignmentPath(stageId, slotKey)}?expectedEntryId={expected}";
        return client.DeleteAsync(path);
    }

    private static Task<HttpResponseMessage> PostCupGenerationAsync(HttpClient client, StageId stageId) =>
        client.PostAsJsonAsync(
            new Uri($"/stages/{stageId.Value}/matches/materialize-from-slots", UriKind.Relative),
            new MaterializeCupFromOccupiedSlotsRequest(PlanEntireRound: true),
            HostJson.Options);

    private static Task<HttpResponseMessage> PostDematerializeAsync(HttpClient client, StageId stageId) =>
        client.PostAsync(new Uri($"/stages/{stageId.Value}/matches/dematerialize", UriKind.Relative), content: null);

    private static string AssignmentPath(StageId stageId, string slotKey) =>
        $"/stages/{stageId.Value}/slots/{Uri.EscapeDataString(slotKey)}/assignment";

    private static Uri MaterializePath(StageId stageId) =>
        new($"/stages/{stageId.Value}/matches/materialize", UriKind.Relative);

    private static Uri MembershipPath(StageId stageId, GroupId groupId) =>
        new($"/stages/{stageId.Value}/groups/{groupId.Value}/membership", UriKind.Relative);

    private static Uri ApplyDrawPath(StageId stageId, Guid drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId}/apply", UriKind.Relative);

    private async Task AssertCupSqlAsync(StageId stageId)
    {
        (await ScalarAsync(
            """
            SELECT COUNT(*)::int FROM (
              SELECT entry_id FROM slots
              WHERE stage_id = @stageId AND entry_id IS NOT NULL
              GROUP BY entry_id HAVING COUNT(*) > 1
            ) d
            """,
            stageId)).Should().Be(0);
        (await ScalarAsync(
            """
            SELECT COUNT(*)::int FROM stage_direct_assignments a
            LEFT JOIN slots s ON s.stage_id = a.stage_id AND s.slot_key = a.slot_key
            WHERE a.stage_id = @stageId AND s.entry_id IS DISTINCT FROM a.entry_id
            """,
            stageId)).Should().Be(0);
        (await ScalarAsync(
            """
            SELECT COUNT(*)::int FROM (
              SELECT f.round_id, f.bracket_pair_key
              FROM fixtures f
              JOIN rounds r ON r.id = f.round_id
              WHERE r.stage_id = @stageId AND f.bracket_pair_key IS NOT NULL
              GROUP BY f.round_id, f.bracket_pair_key
              HAVING COUNT(*) > 1
            ) d
            """,
            stageId)).Should().Be(0);
        (await ScalarAsync(
            """
            SELECT COUNT(*)::int FROM matches m
            JOIN fixture_attachments fa ON fa.match_id = m.id AND fa.leg_index = 1
            JOIN fixtures f ON f.id = fa.fixture_id
            JOIN stage_bracket_pairs p ON p.stage_id = m.stage_id AND p.pair_key = f.bracket_pair_key
            JOIN slots sa ON sa.stage_id = m.stage_id AND sa.slot_key = p.slot_a_key
            JOIN slots sb ON sb.stage_id = m.stage_id AND sb.slot_key = p.slot_b_key
            WHERE m.stage_id = @stageId
              AND (m.home_entry_id IS DISTINCT FROM sa.entry_id
                OR m.away_entry_id IS DISTINCT FROM sb.entry_id)
            """,
            stageId)).Should().Be(0);
    }

    private async Task AssertGroupMatchesFollowMembershipAsync(
        PlayUpWebApplicationFactory factory,
        StageId stageId)
    {
        var (stage, matches) = await ReloadStageAndMatchesAsync(factory, stageId);
        var expected = new HashSet<(Guid, Guid)>();
        foreach (var group in stage.Groups)
        {
            var members = group.EntryIds.Select(id => id.Value).ToArray();
            for (var left = 0; left < members.Length; left++)
            {
                for (var right = left + 1; right < members.Length; right++)
                {
                    expected.Add(NormalizePair(members[left], members[right]));
                }
            }
        }

        var actual = matches
            .Select(match => NormalizePair(match.HomeEntryId!.Value.Value, match.AwayEntryId!.Value.Value))
            .ToArray();
        actual.Should().BeEquivalentTo(expected);
        (await ScalarAsync(
            """
            SELECT COUNT(*)::int FROM matches m
            WHERE m.stage_id = @stageId
              AND NOT EXISTS (
                SELECT 1 FROM groups g
                JOIN group_entries h ON h.group_id = g.id AND h.entry_id = m.home_entry_id
                JOIN group_entries a ON a.group_id = g.id AND a.entry_id = m.away_entry_id
                WHERE g.stage_id = m.stage_id)
            """,
            stageId)).Should().Be(0);
    }

    private async Task<int> ScalarAsync(string sql, StageId stageId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
#pragma warning disable CA2100
        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100
        command.Parameters.AddWithValue(nameof(stageId), stageId.Value);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    private async Task<Guid?> ReadOccupantAsync(StageId stageId, string slotKey)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT entry_id FROM slots WHERE stage_id = @stageId AND slot_key = @slotKey",
            connection);
        command.Parameters.AddWithValue(nameof(stageId), stageId.Value);
        command.Parameters.AddWithValue(nameof(slotKey), slotKey);
        var value = await command.ExecuteScalarAsync();
        return value is Guid id ? id : null;
    }

    private async Task<int> CountOccupantsAsync(StageId stageId, Guid entryId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*)::int FROM slots WHERE stage_id = @stageId AND entry_id = @entryId",
            connection);
        command.Parameters.AddWithValue(nameof(stageId), stageId.Value);
        command.Parameters.AddWithValue(nameof(entryId), entryId);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    private async Task<int> CountMatchesAsync(StageId stageId) =>
        await ScalarAsync("SELECT COUNT(*)::int FROM matches WHERE stage_id = @stageId", stageId);

    private static async Task<Stage> ReloadStageAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        var (stage, _) = await ReloadStageAndMatchesAsync(factory, stageId);
        return stage;
    }

    private static async Task<(Stage Stage, IReadOnlyList<Match> Matches)> ReloadStageAndMatchesAsync(
        PlayUpWebApplicationFactory factory,
        StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var stage = await stages.GetByIdForUpdateAsync(stageId)
                    ?? throw new InvalidOperationException("Stage disappeared.");
        var loaded = await matches.ListByStageForUpdateAsync(stageId);
        return (stage, loaded);
    }

    private static async Task<CallOutcome[]> ReadAsync(HttpResponseMessage[] responses)
    {
        var outcomes = new CallOutcome[responses.Length];
        for (var index = 0; index < responses.Length; index++)
        {
            outcomes[index] = new CallOutcome(responses[index].StatusCode, await ReadCodeAsync(responses[index]));
            responses[index].Dispose();
        }

        return outcomes;
    }

    private static async Task<MaterializeOutcome[]> ReadMaterializeAsync(HttpResponseMessage[] responses)
    {
        var outcomes = new MaterializeOutcome[responses.Length];
        for (var index = 0; index < responses.Length; index++)
        {
            MaterializeMatchesResponse? body = null;
            if (responses[index].StatusCode == HttpStatusCode.OK)
            {
                body = await responses[index].Content.ReadFromJsonAsync<MaterializeMatchesResponse>(HostJson.Options);
            }

            outcomes[index] = new MaterializeOutcome(
                responses[index].StatusCode,
                body?.CreatedCount ?? 0,
                body?.AlreadyComplete ?? false);
            responses[index].Dispose();
        }

        return outcomes;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

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

    private static (Guid Left, Guid Right) NormalizePair(Guid left, Guid right) =>
        left.CompareTo(right) <= 0 ? (left, right) : (right, left);

    private sealed record CupSeed(
        CompetitionId CompetitionId,
        StageId StageId,
        Guid[] Entries,
        string[] OpenSlots,
        DrawId? DrawId,
        string? TargetSlot,
        EntryId? ConflictingEntry);

    private sealed record GroupSeed(CompetitionId CompetitionId, StageId StageId, GroupId GroupId, Guid[] Entries);

    private sealed record CallOutcome(HttpStatusCode Status, string? Code);

    private sealed record MaterializeOutcome(HttpStatusCode Status, int CreatedCount, bool AlreadyComplete);
}
