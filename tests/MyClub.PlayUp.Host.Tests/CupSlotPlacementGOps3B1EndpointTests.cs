// -----------------------------------------------------------------------
// <copyright file="CupSlotPlacementGOps3B1EndpointTests.cs" company="Stéphane ANDRE">
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
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CupSlotPlacementGOps3B1EndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Delete_assignment_when_slot_draw_applied_returns_409_AppliedDrawSlotPlacementProtectedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCupWithAppliedSlotDrawAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync(AssignmentUri(seed.StageId, seed.ProtectedSlot));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.AppliedDrawSlotPlacementProtected);
    }

    [IntegrationFact]
    public async Task Put_assignment_when_slot_draw_applied_changes_protected_entry_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCupWithAppliedSlotDrawAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            AssignmentUri(seed.StageId, seed.ProtectedSlot),
            new AssignEntryToSlotRequest(seed.OtherEntry.Value));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.AppliedDrawSlotPlacementProtected);
    }

    private static Uri AssignmentUri(StageId stageId, string slotKey) =>
        new($"/stages/{stageId.Value}/slots/{Uri.EscapeDataString(slotKey)}/assignment", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<CupB1Seed> SeedCupWithAppliedSlotDrawAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = CreateCompetition.Execute("G-OPS3-B1-host", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;
        var entryIds = competition.Entries.Select(e => e.Id).ToArray();
        stage.ReplaceAffectationAuthoring(entryIds, _clock);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        var slotTargets = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets)
            .IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        DrawAppliedState.IsApplied(draw, stage).Should().BeTrue();

        var protectedSlot = draw.Resolution.SlotResults[0].SlotKey;
        var protectedEntry = draw.Resolution.SlotResults[0].EntryId;
        var otherEntry = entryIds.First(id => !id.Equals(protectedEntry));

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new CupB1Seed(stage.Id, protectedSlot, otherEntry);
    }

    private sealed record CupB1Seed(StageId StageId, string ProtectedSlot, EntryId OtherEntry);
}
