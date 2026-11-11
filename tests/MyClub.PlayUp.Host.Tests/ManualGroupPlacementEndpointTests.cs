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
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
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

    [IntegrationFact]
    public async Task Groups_schematic_projects_seat_modeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsAsync(factory, applyDraw: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri($"/stages/{seed.StageId.Value}/schematic", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var cases = document.RootElement.GetProperty("cases");
        cases.GetArrayLength().Should().BeGreaterThan(0);
        cases.EnumerateArray().Should().OnlyContain(item =>
            item.GetProperty("seatMode").GetString() == "Editable"
            && item.GetProperty("canClear").GetBoolean() == false
            && item.GetProperty("excess").GetBoolean() == false
            && item.GetProperty("outsideComposition").GetBoolean() == false);
    }

    [IntegrationFact]
    public async Task Put_reserved_group_seat_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedReservedGroupAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PutAsync(EntryUri(seed.StageId, seed.GroupId, seed.EntryId), null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be(ApplicationErrorCodes.GroupSeatReservedByInboundFeed);
    }

    [IntegrationFact]
    public async Task Put_and_delete_are_refused_while_a_group_draw_draft_existsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsWithDraftDrawAsync(factory, noSolution: false);
        using var client = factory.CreateClient();

        using var assign = await client.PutAsync(
            EntryUri(seed.StageId, seed.OtherGroupId, seed.SecondEntryId),
            null);
        using var clear = await client.DeleteAsync(EntryUri(seed.StageId, seed.GroupId, seed.EntryId));

        assign.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(assign)).Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        clear.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(clear)).Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
    }

    [IntegrationFact]
    public async Task Put_and_delete_are_refused_for_a_no_solution_group_draw_draftAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedGroupsWithDraftDrawAsync(factory, noSolution: true);
        using var client = factory.CreateClient();

        using var assign = await client.PutAsync(
            EntryUri(seed.StageId, seed.OtherGroupId, seed.SecondEntryId),
            null);
        using var clear = await client.DeleteAsync(EntryUri(seed.StageId, seed.GroupId, seed.EntryId));

        assign.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(assign)).Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
        clear.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(clear)).Should().Be(ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw);
    }

    [IntegrationFact]
    public async Task Put_after_qualification_into_a_freed_group_returns_204Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedQualifiedGroupAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PutAsync(EntryUri(seed.StageId, seed.GroupId, seed.EntryId), null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [IntegrationFact]
    public async Task Groups_schematic_projects_draw_pending_and_cup_omits_seat_fieldsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var draft = await SeedGroupsWithDraftDrawAsync(factory, noSolution: true);
        var cupId = await SeedCupAsync(factory);
        using var client = factory.CreateClient();

        using var groups = await client.GetAsync(SchematicUri(draft.StageId));
        using var cup = await client.GetAsync(SchematicUri(cupId));

        groups.StatusCode.Should().Be(HttpStatusCode.OK);
        using var groupsDocument = await JsonDocument.ParseAsync(await groups.Content.ReadAsStreamAsync());
        groupsDocument.RootElement.GetProperty("cases").EnumerateArray().Should()
            .OnlyContain(item => item.GetProperty("seatMode").GetString() == "DrawPending");

        cup.StatusCode.Should().Be(HttpStatusCode.OK);
        using var cupDocument = await JsonDocument.ParseAsync(await cup.Content.ReadAsStreamAsync());
        var cupCases = cupDocument.RootElement.GetProperty("cases");
        cupCases.GetArrayLength().Should().BeGreaterThan(0);
        foreach (var item in cupCases.EnumerateArray())
        {
            item.TryGetProperty("seatMode", out _).Should().BeFalse();
            item.TryGetProperty("canClear", out _).Should().BeFalse();
            item.TryGetProperty("excess", out _).Should().BeFalse();
            item.TryGetProperty("outsideComposition", out _).Should().BeFalse();
        }
    }

    private static Uri SchematicUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/schematic", UriKind.Relative);

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

    private async Task<GroupSeed> SeedReservedGroupAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("G-http-reserved", _clock);
        var entryId = AddEntry.Execute(competition, "Manual", _clock).Id;
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        destination.ReplaceAffectationAuthoring([entryId], _clock);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(source.Groups[0].Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id)),
                new QualificationPath(
                    2,
                    QualificationSource.FromGroup(source.Groups[1].Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id))
            ]),
            _clock);

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        stages.Add(source);
        stages.Add(destination);
        await unitOfWork.SaveChangesAsync();

        return new GroupSeed(
            destination.Id,
            destination.Groups[0].Id,
            destination.Groups[1].Id,
            entryId,
            entryId,
            entryId,
            entryId,
            entryId);
    }

    private async Task<GroupSeed> SeedGroupsWithDraftDrawAsync(
        PlayUpWebApplicationFactory factory,
        bool noSolution)
    {
        var competition = CreateCompetition.Execute(noSolution ? "G-http-orphan" : "G-http-draft", _clock);
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
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        if (noSolution)
        {
            stage.MarkDrawNoSolution(draw.Id, _clock);
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
            stage.Groups[1].Id,
            ids[0],
            ids[1],
            ids[2],
            ids[3],
            ids[0]);
    }

    private async Task<GroupSeed> SeedQualifiedGroupAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("G-http-qualified", _clock);
        var manualId = AddEntry.Execute(competition, "Manual", _clock).Id;
        var sourceIds = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"S{index}", _clock).Id)
            .ToArray();
        var source = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2), _clock);
        var destination = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 4), _clock);
        destination.ReplaceAffectationAuthoring([manualId], _clock);
        source.AssignEntryToGroup(source.Groups[0].Id, sourceIds[0]);
        source.AssignEntryToGroup(source.Groups[0].Id, sourceIds[1]);
        source.AssignEntryToGroup(source.Groups[1].Id, sourceIds[2]);
        source.AssignEntryToGroup(source.Groups[1].Id, sourceIds[3]);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(source.Groups[0].Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id)),
                new QualificationPath(
                    2,
                    QualificationSource.FromGroup(source.Groups[1].Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForGroup(destination.Id, destination.Groups[0].Id))
            ]),
            _clock);
        var matches = new List<Match>
        {
            Finish(Match.Create(competition.Id, source.Id, sourceIds[0], sourceIds[1], _clock), 2, 0),
            Finish(Match.Create(competition.Id, source.Id, sourceIds[2], sourceIds[3], _clock), 1, 0)
        };
        var rules = source.Regulation.StandingRules
            ?? throw new InvalidOperationException("Standing rules are required.");
        var standings = new Dictionary<GroupId, Standing>
        {
            [source.Groups[0].Id] = CalculateStanding.Execute(source.Groups[0].EntryIds, matches, rules),
            [source.Groups[1].Id] = CalculateStanding.Execute(source.Groups[1].EntryIds, matches, rules)
        };
        ApplyQualification.Execute(source, overallStanding: null, standings, [source, destination], _clock);

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        stages.Add(source);
        stages.Add(destination);
        await unitOfWork.SaveChangesAsync();

        return new GroupSeed(
            destination.Id,
            destination.Groups[0].Id,
            destination.Groups[1].Id,
            manualId,
            manualId,
            manualId,
            manualId,
            manualId);
    }

    private async Task<StageId> SeedCupAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("Cup-http-schematic", _clock);
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock).Stage;

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();
        return stage.Id;
    }

    private Match Finish(Match match, int homeGoals, int awayGoals)
    {
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
        return match;
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
