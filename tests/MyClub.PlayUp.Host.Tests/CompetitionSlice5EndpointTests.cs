// -----------------------------------------------------------------------
// <copyright file="CompetitionSlice5EndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionSlice5EndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Apply_qualification_fills_destination_slotsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var (leagueId, terminalId) = await SeedChampionshipQualificationAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/stages/{leagueId.Value}/qualification/apply",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var terminal = await stages.GetByIdForUpdateAsync(terminalId);
        terminal!.FindSlot("Champ")!.EntryId.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task Apply_progression_conflict_returns_409_without_overwriteAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedFinishedProgressionWithForeignOccupantAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/stages/{seed.StageId.Value}/fixtures/{seed.FixtureId.Value}/apply-progression",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.SlotOccupancyConflict);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdForUpdateAsync(seed.StageId);
        stage!.FindSlot("SF1-A")!.EntryId.Should().Be(seed.ForeignEntryId);
    }

    [IntegrationFact]
    public async Task Attention_and_workspace_count_reflect_progression_pendingAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedFinishedProgressionPendingAsync(factory);
        using var client = factory.CreateClient();

        using var attentionResponse = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/attention");
        attentionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attention = await attentionResponse.Content.ReadFromJsonAsync<NeedsAttentionDto>(HostJson.Options);
        attention!.Count.Should().BeGreaterThan(0);
        attention.Items.Should().Contain(item => item.Source == NeedsAttentionAssembler.SourceProgressionPending);

        using var workspaceResponse = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/workspace");
        workspaceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options);
        workspace!.AttentionCount.Should().Be(attention.Count);
    }

    [IntegrationFact]
    public async Task Apply_qualification_when_competition_completed_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var (leagueId, _) = await SeedChampionshipQualificationAsync(factory, completeCompetition: true);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/stages/{leagueId.Value}/qualification/apply",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.ConsequenceOperationNotAllowed);
    }

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<(StageId LeagueId, StageId TerminalId)> SeedChampionshipQualificationAsync(
        PlayUpWebApplicationFactory factory,
        bool completeCompetition = false)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Slice5 Qualif"), SampleRegulations.Standard(), _clock);
        var e1 = competition.AddEntry(TeamId.New(), "A", _clock);
        var e2 = competition.AddEntry(TeamId.New(), "B", _clock);
        var e3 = competition.AddEntry(TeamId.New(), "C", _clock);
        competitions.Add(competition);

        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var md = league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);

        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ", _clock);
        competition.AddStage(terminal.Id, _clock);

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(terminal.Id, "Champ"))
            ]),
            _clock);

        var entries = new[] { e1.Id, e2.Id, e3.Id };
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var addFixture = league.AddFixture(md.Id, _clock);
                var match = Match.Create(competition.Id, league.Id, entries[i], entries[j], _clock);
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
                league.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
                matches.Add(match);
            }
        }

        stages.Add(league);
        stages.Add(terminal);

        if (completeCompetition)
        {
            competition.Prepare(_clock);
            competition.Start(_clock);
            competition.Complete(CompletionMode.Administrative, _clock);
        }

        await unitOfWork.SaveChangesAsync();
        return (league.Id, terminal.Id);
    }

    private async Task<(CompetitionId CompetitionId, StageId StageId, FixtureId FixtureId)>
        SeedFinishedProgressionPendingAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Slice5 Prog"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A", _clock);
        competition.AddStage(stage.Id, _clock);

        var addFixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(3, 1)), _clock);
        stage.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    addFixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return (competition.Id, stage.Id, addFixture.Id);
    }

    private async Task<(StageId StageId, FixtureId FixtureId, EntryId ForeignEntryId)>
        SeedFinishedProgressionWithForeignOccupantAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Slice5 Conflict"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var foreign = competition.AddEntry(TeamId.New(), "Foreign", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A", _clock);
        competition.AddStage(stage.Id, _clock);

        var addFixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        stage.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    addFixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);
        stage.ApplyResolvedEntry("SF1-A", foreign.Id, _clock);

        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return (stage.Id, addFixture.Id, foreign.Id);
    }
}
