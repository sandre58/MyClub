// -----------------------------------------------------------------------
// <copyright file="RedoDrawEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Text;
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
using MyClub.PlayUp.Host.Contracts;
using Npgsql;
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
        body.DrawId.Should().NotBe(seed.DrawId.Value);
        body.IsResolved.Should().BeTrue();
        body.IsNoSolution.Should().BeFalse();
        body.Status.Should().Be(DrawStatus.Draft);
        body.ResolutionState.Should().Be(DrawResolutionState.Resolved);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Cancelled);
        stage.GetDraw(new DrawId(body.DrawId)).Status.Should().Be(DrawStatus.Draft);
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        stage.FindSlot("S2")!.EntryId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Redo_when_a_direct_assignment_is_present_on_partial_draw_returns_200Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedPartialSlotDrawWithDirectAssignmentAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DrawGenerationDto>(HostJson.Options);
        body.Should().NotBeNull();
        body.DrawId.Should().NotBe(seed.DrawId.Value);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Cancelled);
        stage.GetDraw(new DrawId(body!.DrawId)).InputsIntent.Should().Be(DrawInputsIntent.Partial);
        stage.FindSlot("S2")!.EntryId.Should().Be(seed.DirectEntry);
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        stage.GetDraw(new DrawId(body.DrawId)).Resolution.SlotResults
            .Should().NotContain(placement => placement.SlotKey == "S2");
    }

    [IntegrationFact]
    public async Task Redo_when_the_draw_is_not_applied_returns_409_and_changes_nothingAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedDraftSlotDrawAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.DrawRedoRequiresApplied);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Draft);
        stage.Draws.Should().ContainSingle();
        stage.FindSlot("S1")!.EntryId.Should().BeNull();
        stage.FindSlot("S2")!.EntryId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Redo_when_a_sibling_stage_feed_targets_the_grid_returns_409_and_changes_nothingAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedSlotDrawWithSiblingFeedAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.DrawLaunchGridNotVacant);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage.Should().NotBeNull();
        stage.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Published);
        stage.Draws.Should().ContainSingle();
        stage.FindSlot("SF1-A")!.EntryId.Should().Be(seed.SlotS1Entry);
        stage.FindSlot("SF1-B")!.EntryId.Should().Be(seed.SlotS2Entry);
    }

    private async Task<PartialSlotDrawSeed> SeedAppliedPartialSlotDrawWithDirectAssignmentAsync(
        PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Redo Partial Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.SeedEntryRoundBracketPairs();
        ReplaceStageDrawRules.Execute(stage, competition, new DrawRules(DrawMode.Random), _clock);
        var drawn = EntryId.New();
        var manual = EntryId.New();
        stage.ReplaceAffectationAuthoring([drawn, manual], _clock);
        stage.AssignEntryToSlot("S2", manual);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreatePartial(stage, [stage], DrawResolutionKind.Slot),
            DrawInputsIntent.Partial,
            [stage]);
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets: ["S1", "S2"], competitionStages: [stage])
            .IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new PartialSlotDrawSeed(competition.Id, stage.Id, draw.Id, manual);
    }

    [IntegrationFact]
    public async Task Redo_when_all_sibling_feeds_reserve_released_slots_returns_409_DrawNoFreeSlotAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedSlotDrawWithSiblingFeedAsync(factory, reserveBothSlots: true);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadCodeAsync(response)).Should().Be(ApplicationErrorCodes.DrawNoFreeSlot);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage!.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Published);
    }

    [IntegrationFact]
    public async Task Redo_then_configure_partial_and_generate_match_redo_perimeterAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedPartialSlotDrawWithDirectAssignmentAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DrawGenerationDto>(HostJson.Options);
        body.Should().NotBeNull();
        var newDrawId = new DrawId(body!.DrawId);

        using (var inputs = await client.PostAsJsonAsync(
                   InputsUri(seed.StageId, newDrawId),
                   new ConfigureDrawInputsRequest("Partial")))
        {
            inputs.EnsureSuccessStatusCode();
        }

        using (var generate = await client.PostAsync(GenerateUri(seed.StageId, newDrawId), content: null))
        {
            generate.EnsureSuccessStatusCode();
        }

        var stage = await ReloadAsync(factory, seed.StageId);
        var preview = stage.GetDraw(newDrawId);
        preview.InputsIntent.Should().Be(DrawInputsIntent.Partial);
        preview.Resolution.SlotResults.Should().NotContain(placement => placement.SlotKey == "S2");
        preview.Resolution.SlotResults.Should().ContainSingle();
        stage.FindSlot("S2")!.EntryId.Should().Be(seed.DirectEntry);
    }

    [IntegrationFact]
    public async Task Redo_then_assign_on_preview_target_apply_returns_400_and_persists_nothingAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedPartialSlotDrawWithDirectAssignmentAsync(factory);
        using var client = factory.CreateClient();
        var before = await ReloadAsync(factory, seed.StageId);
        var manualBefore = before.FindSlot("S2")!.EntryId;

        using var redo = await client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null);
        redo.EnsureSuccessStatusCode();
        var body = await redo.Content.ReadFromJsonAsync<DrawGenerationDto>(HostJson.Options);
        var newDrawId = body!.DrawId;
        var intermediate = await ReloadAsync(factory, seed.StageId);
        var targetPlacement = intermediate.GetDraw(new DrawId(newDrawId)).Resolution.SlotResults[0];
        var targetKey = targetPlacement.SlotKey;
        intermediate.FindSlot(targetKey)!.EntryId.Should().BeNull();

        using (var clear = await client.DeleteAsync(
                   $"{AssignmentPath(seed.StageId, "S2")}?expectedEntryId={seed.DirectEntry.Value}"))
        {
            clear.EnsureSuccessStatusCode();
        }

        (await PutSlotAsync(
                client,
                seed.StageId,
                targetKey,
                seed.DirectEntry.Value,
                expected: null))
            .EnsureSuccessStatusCode();

        using (var publish = await client.PostAsync(PublishUri(seed.StageId, newDrawId), content: null))
        {
            publish.EnsureSuccessStatusCode();
        }

        using var apply = await client.PostAsync(ApplyUri(seed.StageId, newDrawId), content: null);
        apply.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadCodeAsync(apply)).Should().Be(ApplicationErrorCodes.DrawApplyFailure);

        var after = await ReloadAsync(factory, seed.StageId);
        after.FindSlot("S2")!.EntryId.Should().BeNull();
        after.FindSlot("S1")!.EntryId.Should().Be(seed.DirectEntry);
        after.DirectAssignments.Should().ContainSingle(assignment => assignment.SlotKey == targetKey);
        after.GetDraw(new DrawId(newDrawId)).Status.Should().Be(DrawStatus.Published);
        after.GetDraw(new DrawId(newDrawId)).Resolution.SlotResults
            .Should().Contain(placement => placement.SlotKey == targetKey);
    }

    [IntegrationFact]
    public async Task Redo_parallel_with_assignment_outside_preview_places_nothing_on_direct_assignmentsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedPartialSlotDrawWithDirectAssignmentAsync(factory);
        using var client = factory.CreateClient();
        var outsideEntry = EntryId.New();
        var responses = await OverlapOnCompetitionLockAsync(
            seed.CompetitionId,
            () => client.PostAsync(RedoUri(seed.StageId, seed.DrawId), content: null),
            () => PutSlotAsync(client, seed.StageId, "S2", outsideEntry.Value, expected: seed.DirectEntry.Value));
        responses.Should().Contain(response => response.StatusCode == HttpStatusCode.OK);

        var stage = await ReloadAsync(factory, seed.StageId);
        var preview = stage.Draws.Single(draw => draw.Status == DrawStatus.Draft);
        preview.Resolution.SlotResults.Should().NotContain(placement => placement.SlotKey == "S2");
        stage.FindSlot("S2")!.EntryId.Should().BeOneOf(seed.DirectEntry, outsideEntry);
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
        stage.ReplaceAffectationAuthoring([entryA, entryB], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets: ["S1", "S2"])
            .IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        if (withUnrelatedDirectAssignment)
        {
            // Added after this draw is generated and applied so the draw's own entry pool stays
            // [entryA, entryB] (matching its 2 slot targets); the direct assignment on a brand new
            // slot is unrelated to this draw, which is exactly the scenario being exercised.
            stage.AddSlot("S3");
            var directEntry = EntryId.New();
            stage.ReplaceAffectationAuthoring([entryA, entryB, directEntry], _clock);
            stage.AssignEntryToSlot("S3", directEntry);
        }

        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        // DrawMode.Random: capture the actual occupancy rather than assuming entryA -> S1.
        var slotS1Entry = stage.FindSlot("S1")!.EntryId!.Value;
        var slotS2Entry = stage.FindSlot("S2")!.EntryId!.Value;
        return new SlotDrawSeed(stage.Id, draw.Id, slotS1Entry, slotS2Entry);
    }

    private async Task<SlotDrawSeed> SeedDraftSlotDrawAsync(PlayUpWebApplicationFactory factory)
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
        stage.ReplaceAffectationAuthoring([entryA, entryB], _clock);

        // Left Draft on purpose: never published nor applied, so the grid stays vacant.
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new SlotDrawSeed(stage.Id, draw.Id, SlotS1Entry: null, SlotS2Entry: null);
    }

    private async Task<SlotDrawSeed> SeedAppliedSlotDrawWithSiblingFeedAsync(
        PlayUpWebApplicationFactory factory,
        bool reserveBothSlots = false)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Redo Draw Cup"), SampleRegulations.Standard(), _clock);
        var source = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        target.AddRound("R1", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");
        target.SeedEntryRoundBracketPairs();
        ReplaceStageDrawRules.Execute(target, competition, new DrawRules(DrawMode.Random), _clock);
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        target.ReplaceAffectationAuthoring([entryA, entryB], _clock);

        var draw = CreateDraw.Execute(target, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(target, draw.Id, DrawInputsFactory.CreateDefault(target, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(target, draw.Id, _clock, slotTargets: ["SF1-A", "SF1-B"])
            .IsResolved.Should().BeTrue();
        PublishDraw.Execute(target, draw.Id, _clock);
        ApplyDraw.Execute(target, draw.Id, _clock);

        // The sibling `source` stage feeds a slot that `target`'s draw already occupies. The upfront
        // single-stage evaluator never sees this (it reports Redo as Available); only the real Rerun
        // launch guard — fed the full competition stages inside UseCaseExecutor.RedoDrawAsync — catches
        // it, mid-sequence, after Cancel + Release already ran in-memory.
        var paths = new List<QualificationPath>
        {
            new(
                1,
                QualificationSource.Overall(),
                new QualificationSelection(SelectionMode.Position, 1),
                QualificationDestination.ForSlot(target.Id, "SF1-A"))
        };
        if (reserveBothSlots)
        {
            paths.Add(new QualificationPath(
                2,
                QualificationSource.Overall(),
                new QualificationSelection(SelectionMode.Position, 2),
                QualificationDestination.ForSlot(target.Id, "SF1-B")));
        }

        source.ReplaceQualificationRules(new QualificationRules(paths), _clock);

        competition.AddStage(source.Id, _clock);
        competition.AddStage(target.Id, _clock);
        competitions.Add(competition);
        stages.Add(source);
        stages.Add(target);
        await unitOfWork.SaveChangesAsync();

        // DrawMode.Random: capture the actual occupancy rather than assuming entryA -> SF1-A.
        var slotAEntry = target.FindSlot("SF1-A")!.EntryId!.Value;
        var slotBEntry = target.FindSlot("SF1-B")!.EntryId!.Value;
        return new SlotDrawSeed(target.Id, draw.Id, slotAEntry, slotBEntry);
    }

    private static async Task<Stage> ReloadAsync(PlayUpWebApplicationFactory factory, StageId stageId)
    {
        using var scope = factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(stageId))!;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        return problem is null ? null : GetCode(problem);
    }

    private static string InputsUri(StageId stageId, DrawId drawId) =>
        $"/stages/{stageId.Value}/draws/{drawId.Value}/inputs";

    private static Uri GenerateUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/generate", UriKind.Relative);

    private static Uri PublishAndApplyUri(StageId stageId, Guid drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId}/publish-and-apply", UriKind.Relative);

    private static Uri PublishUri(StageId stageId, Guid drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId}/publish", UriKind.Relative);

    private static Uri ApplyUri(StageId stageId, Guid drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId}/apply", UriKind.Relative);

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

    private static string AssignmentPath(StageId stageId, string slotKey) =>
        $"/stages/{stageId.Value}/slots/{Uri.EscapeDataString(slotKey)}/assignment";

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

    private sealed record SlotDrawSeed(StageId StageId, DrawId DrawId, EntryId? SlotS1Entry, EntryId? SlotS2Entry);

    private sealed record PartialSlotDrawSeed(
        CompetitionId CompetitionId,
        StageId StageId,
        DrawId DrawId,
        EntryId DirectEntry);
}
