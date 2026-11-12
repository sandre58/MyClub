// -----------------------------------------------------------------------
// <copyright file="DefinitiveResolutionReleaseEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class DefinitiveResolutionReleaseEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Preview_then_confirm_clears_the_definitive_placeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedAsync(factory);
        using var client = factory.CreateClient();

        var before = await ReadOccupantsAsync(client, seeded.CompetitionId);
        before.Should().Contain(occupant =>
            occupant.SlotKey == "Champ" && occupant.Definitive && occupant.Releasable);

        var preview = await client.PostAsJsonAsync(
            $"/stages/{seeded.LeagueId}/resolution/release-definitive-preview",
            new DefinitiveResolutionReleasePreviewRequest(seeded.TerminalId),
            HostJson.Options);
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await preview.Content.ReadFromJsonAsync<DefinitiveResolutionReleaseResponse>(HostJson.Options);
        body.Should().NotBeNull();
        body.Releases.Should().Contain(row =>
            row.SlotKey == "Champ"
            && row.EntryName == "Nord"
            && row.StageName == "Terminal"
            && row.PlaceLabel == "Champ"
            && row.PopulationRemoved
            && !row.SideEffect);
        body.Releases.Should().Contain(row =>
            row.Target == StructureResolutionOccupantTarget.Population && row.SideEffect && row.EntryName == "Nord");

        var confirm = await ConfirmAsync(client, seeded.LeagueId, body);
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmed = await confirm.Content.ReadFromJsonAsync<DefinitiveResolutionReleaseResponse>(HostJson.Options);
        confirmed!.Releases.Should().Contain(row => row.SlotKey == "Champ" && row.EntryName == "Nord");
        (await ReadOccupantsAsync(client, seeded.CompetitionId))
            .Should().NotContain(occupant => occupant.SlotKey == "Champ");

        var again = await ConfirmAsync(client, seeded.LeagueId, body);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await again.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(ApplicationErrorCodes.ResolutionReleaseConflict);
    }

    [IntegrationFact]
    public async Task An_unknown_source_is_not_found_and_a_bad_body_is_rejectedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedAsync(factory);
        using var client = factory.CreateClient();

        var missing = await client.PostAsJsonAsync(
            $"/stages/{Guid.NewGuid()}/resolution/release-definitive-preview",
            new DefinitiveResolutionReleasePreviewRequest(null),
            HostJson.Options);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        GetCode(await missing.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(ApplicationErrorCodes.StageNotFound);

        using var malformed = new StringContent("{", Encoding.UTF8, "application/json");
        var broken = await client.PostAsync(
            $"/stages/{seeded.LeagueId}/resolution/release-definitive",
            malformed);
        broken.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var missingPlaces = await client.PostAsJsonAsync(
            $"/stages/{seeded.LeagueId}/resolution/release-definitive",
            new { destinationStageId = seeded.TerminalId, versionToken = "abc" },
            HostJson.Options);
        missingPlaces.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var place = new DefinitiveResolutionReleasePlaceRequest(
            seeded.TerminalId,
            StructureResolutionOccupantTarget.Slot,
            seeded.EntryId,
            "Champ",
            null);
        var blank = await client.PostAsJsonAsync(
            $"/stages/{seeded.LeagueId}/resolution/release-definitive",
            new DefinitiveResolutionReleaseConfirmRequest(seeded.TerminalId, " ", [place]),
            HostJson.Options);
        blank.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await blank.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(ApplicationErrorCodes.ResolutionReleaseConflict);
        (await ReadOccupantsAsync(client, seeded.CompetitionId))
            .Should().Contain(occupant => occupant.SlotKey == "Champ");
    }

    private async Task<Seed> SeedAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = Competition.Create(
            new CompetitionName($"Definitive {Guid.NewGuid():N}"),
            SampleRegulations.Standard(),
            _clock);
        var nord = competition.AddEntry(TeamId.New(), "Nord", _clock).Id;
        var sud = competition.AddEntry(TeamId.New(), "Sud", _clock).Id;
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 99),
            QualificationDestination.ForSlot(terminal.Id, "Champ"));
        league.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");
        terminal.AddResolvedPopulationEntry(nord, _clock, provenance);
        terminal.ApplyResolvedEntry("Champ", nord, _clock, provenance);
        var match = Match.Create(competition.Id, league.Id, nord, sud, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        league.Prepare(_clock);
        league.Start(_clock);
        league.Complete(_clock);

        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().Add(competition);
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        stages.Add(league);
        stages.Add(terminal);
        scope.ServiceProvider.GetRequiredService<IMatchRepository>().Add(match);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return new Seed(competition.Id.Value, league.Id.Value, terminal.Id.Value, nord.Value);
    }

    private static async Task<HttpResponseMessage> ConfirmAsync(
        HttpClient client,
        Guid sourceId,
        DefinitiveResolutionReleaseResponse preview)
    {
        var places = preview.Releases
            .Where(row => !row.SideEffect)
            .Select(row => new DefinitiveResolutionReleasePlaceRequest(
                row.StageId, row.Target, row.EntryId, row.SlotKey, row.GroupId))
            .ToArray();
        return await client.PostAsJsonAsync(
            $"/stages/{sourceId}/resolution/release-definitive",
            new DefinitiveResolutionReleaseConfirmRequest(preview.Releases[0].StageId, preview.VersionToken, places),
            HostJson.Options);
    }

    private static async Task<IReadOnlyList<StructureResolutionOccupantDto>> ReadOccupantsAsync(
        HttpClient client,
        Guid competitionId)
    {
        using var response = await client.GetAsync($"/competitions/{competitionId}/structure");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await response.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        view.Should().NotBeNull();
        return view.Stages.SelectMany(stage => stage.ResolutionOccupants ?? []).ToArray();
    }

    private static string? GetCode(ProblemDetails? problem) =>
        problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private sealed record Seed(Guid CompetitionId, Guid LeagueId, Guid TerminalId, Guid EntryId);
}
