// -----------------------------------------------------------------------
// <copyright file="CompetitionSlice7EndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionSlice7EndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 17, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Consultation_running_returns_results_standings_structureAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedFinishedChampionshipAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/consultation");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var view = await response.Content.ReadFromJsonAsync<ConsultationViewDto>(HostJson.Options);
        view.Should().NotBeNull();
        view!.Status.Should().Be(CompetitionStatus.Running);
        view.FormatKind.Should().Be(StructureFormatKind.Championship);
        view.Results.Should().HaveCount(3);
        view.Results.Should().OnlyContain(r => r.Score != null && r.MatchId != Guid.Empty);
        view.Standings.Applicable.Should().BeTrue();
        view.Standings.Tables.Should().ContainSingle();
        view.Standings.Tables[0].Rows.Should().HaveCount(3);
        view.Structure.Stages.Should().ContainSingle();
        view.Structure.Stages[0].Matchdays.Should().NotBeEmpty();
    }

    [IntegrationFact]
    public async Task Consultation_completed_and_archived_remain_readableAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedFinishedChampionshipAsync(factory, complete: true);
        using var client = factory.CreateClient();

        using var completedResponse = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/consultation");
        completedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await completedResponse.Content.ReadFromJsonAsync<ConsultationViewDto>(HostJson.Options);
        completed!.Status.Should().Be(CompetitionStatus.Completed);
        completed.CompletionMode.Should().Be(CompletionMode.Normal);
        completed.Results.Should().NotBeEmpty();

        using var archiveResponse = await client.PostAsync(
            $"/competitions/{seed.CompetitionId.Value}/archive",
            content: null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var archivedResponse = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/consultation");
        archivedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archived = await archivedResponse.Content.ReadFromJsonAsync<ConsultationViewDto>(HostJson.Options);
        archived!.Status.Should().Be(CompetitionStatus.Archived);
        archived.Results.Should().HaveCount(completed.Results.Count);
        archived.Standings.Applicable.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Consultation_missing_competition_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{Guid.NewGuid()}/consultation");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task Consultation_is_read_only_does_not_change_statusAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedFinishedChampionshipAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/consultation");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider
            .GetRequiredService<ICompetitionRepository>()
            .GetByIdAsync(seed.CompetitionId);
        loaded!.Status.Should().Be(CompetitionStatus.Running);
        loaded.CompletionMode.Should().BeNull();
    }

    private async Task<(CompetitionId CompetitionId, StageId StageId)> SeedFinishedChampionshipAsync(
        PlayUpWebApplicationFactory factory,
        bool complete = false)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Slice7 League"), SampleRegulations.Standard(), _clock);
        var e1 = competition.AddEntry(TeamId.New(), "A", _clock);
        var e2 = competition.AddEntry(TeamId.New(), "B", _clock);
        var e3 = competition.AddEntry(TeamId.New(), "C", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var md = stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var entries = new[] { e1.Id, e2.Id, e3.Id };
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var fixture = stage.AddFixture(md.Id, _clock);
                var match = Match.Create(competition.Id, stage.Id, entries[i], entries[j], _clock);
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
                stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
                matches.Add(match);
            }
        }

        stages.Add(stage);

        if (complete)
        {
            competition.Complete(CompletionMode.Normal, _clock);
        }

        await unitOfWork.SaveChangesAsync();
        return (competition.Id, stage.Id);
    }
}
