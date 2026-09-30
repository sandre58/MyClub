// -----------------------------------------------------------------------
// <copyright file="MatchSheetEndpointTests.cs" company="Stéphane ANDRE">
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
public sealed class MatchSheetEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 31, 9, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Match_sheet_live_and_facts_flow_persists_and_surfaces_in_detailAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedScheduledMatchWithRosterAsync(factory);
        using var client = factory.CreateClient();

        using (var addStarter = await client.PostAsJsonAsync(
            AddParticipationUri(seed.MatchId),
            new AddDeclaredParticipationRequest(
                seed.DupontId.Value,
                Side.Home,
                CompositionStatus.Starter,
                JerseyNumber: 9)))
        {
            addStarter.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var addBench = await client.PostAsJsonAsync(
            AddParticipationUri(seed.MatchId),
            new AddDeclaredParticipationRequest(
                seed.MartinId.Value,
                Side.Home,
                CompositionStatus.Bench)))
        {
            addBench.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var addAway = await client.PostAsJsonAsync(
            AddParticipationUri(seed.MatchId),
            new AddDeclaredParticipationRequest(
                seed.RivalId.Value,
                Side.Away,
                CompositionStatus.Starter)))
        {
            addAway.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var start = await client.PostAsync(StartUri(seed.MatchId), content: null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var runningScore = await client.PutAsJsonAsync(
            RunningScoreUri(seed.MatchId),
            new SetRunningScoreRequest(2, 0)))
        {
            runningScore.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var goal = await client.PostAsJsonAsync(
            RecordedGoalsUri(seed.MatchId),
            new RecordGoalRequest(seed.DupontId.Value, Side.Home, seed.MartinId.Value)))
        {
            goal.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var substitution = await client.PostAsJsonAsync(
            RecordedSubstitutionsUri(seed.MatchId),
            new RecordSubstitutionRequest(seed.DupontId.Value, seed.MartinId.Value, Side.Home)))
        {
            substitution.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var disciplinary = await client.PostAsJsonAsync(
            RecordedDisciplinaryEventsUri(seed.MatchId),
            new RecordDisciplinaryEventRequest(seed.RivalId.Value, DisciplinaryType.Yellow)))
        {
            disciplinary.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var detailResponse = await client.GetAsync($"/matches/{seed.MatchId.Value}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
        detail.Should().NotBeNull();
        detail.HasObservedLive.Should().BeTrue();
        detail.RunningScore.Should().Be(new MatchScoreDto(2, 0));
        detail.DeclaredParticipations.Should().HaveCount(3);
        detail.RecordedGoals.Should().ContainSingle();
        detail.RecordedGoals[0].ScorerDisplayName.Should().Be("Dupont");
        detail.RecordedGoals[0].AssisterDisplayName.Should().Be("Martin");
        detail.RecordedSubstitutions.Should().ContainSingle();
        detail.RecordedDisciplinaryEvents.Should().ContainSingle();
        detail.RecordedDisciplinaryEvents[0].MemberDisplayName.Should().Be("Rival");

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(seed.MatchId);
        loaded!.DeclaredParticipations.Should().HaveCount(3);
        loaded.RecordedGoals.Should().ContainSingle();
        loaded.RecordedSubstitutions.Should().ContainSingle();
        loaded.RecordedDisciplinaryEvents.Should().ContainSingle();
        loaded.RunningScore.Should().Be(new RunningScore(2, 0));
    }

    [IntegrationFact]
    public async Task AddDeclaredParticipation_when_match_missing_returns_404_MatchNotFoundAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            AddParticipationUri(MatchId.New()),
            new AddDeclaredParticipationRequest(
                Guid.CreateVersion7(),
                Side.Home,
                CompositionStatus.Starter));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(ApplicationErrorCodes.MatchNotFound);
    }

    [IntegrationFact]
    public async Task AddDeclaredParticipation_when_member_not_eligible_returns_409_ParticipationNotEligibleAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedScheduledMatchWithRosterAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            AddParticipationUri(seed.MatchId),
            new AddDeclaredParticipationRequest(
                Guid.CreateVersion7(),
                Side.Home,
                CompositionStatus.Starter));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(ApplicationErrorCodes.ParticipationNotEligible);
    }

    [IntegrationFact]
    public async Task SetRunningScore_when_scheduled_returns_409_InvalidTransitionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedScheduledMatchWithRosterAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            RunningScoreUri(seed.MatchId),
            new SetRunningScoreRequest(1, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(MatchErrorCodes.RunningScoreImmutable);
    }

    [IntegrationFact]
    public async Task RemoveRecordedGoal_on_live_match_persistsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedLiveMatchWithSheetAsync(factory);
        using var client = factory.CreateClient();

        GoalId goalId;
        using (var scope = factory.Services.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(seed.MatchId);
            goalId = loaded!.RecordedGoals.Single().Id;
        }

        using (var removeGoal = await client.DeleteAsync(RecordedGoalUri(seed.MatchId, goalId)))
        {
            removeGoal.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var detailResponse = await client.GetAsync($"/matches/{seed.MatchId.Value}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
        detail!.RecordedGoals.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task DeclaredParticipation_mutations_on_scheduled_match_persistAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedScheduledMatchWithSheetAsync(factory);
        using var client = factory.CreateClient();

        using (var removeParticipation = await client.DeleteAsync(
            ParticipationUri(seed.MatchId, seed.MartinId)))
        {
            removeParticipation.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var changeStatus = await client.PutAsJsonAsync(
            CompositionStatusUri(seed.MatchId, seed.DupontId),
            new ChangeDeclaredParticipationCompositionStatusRequest(CompositionStatus.Bench)))
        {
            changeStatus.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var jersey = await client.PutAsJsonAsync(
            JerseyNumberUri(seed.MatchId, seed.DupontId),
            new SetDeclaredParticipationJerseyNumberRequest(10)))
        {
            jersey.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var detailResponse = await client.GetAsync($"/matches/{seed.MatchId.Value}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
        detail!.DeclaredParticipations.Should().HaveCount(2);
        detail.DeclaredParticipations.Should().NotContain(p => p.MemberId == seed.MartinId.Value);
        detail.DeclaredParticipations.Should().ContainSingle(p =>
            p.MemberId == seed.DupontId.Value
            && p.CompositionStatus == CompositionStatus.Bench
            && p.JerseyNumber == 10);
    }

    private static Uri StartUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/start", UriKind.Relative);

    private static Uri AddParticipationUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/declared-participations", UriKind.Relative);

    private static Uri ParticipationUri(MatchId matchId, MemberId memberId) =>
        new($"/matches/{matchId.Value}/declared-participations/{memberId.Value}", UriKind.Relative);

    private static Uri CompositionStatusUri(MatchId matchId, MemberId memberId) =>
        new($"/matches/{matchId.Value}/declared-participations/{memberId.Value}/composition-status", UriKind.Relative);

    private static Uri JerseyNumberUri(MatchId matchId, MemberId memberId) =>
        new($"/matches/{matchId.Value}/declared-participations/{memberId.Value}/jersey-number", UriKind.Relative);

    private static Uri RunningScoreUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/running-score", UriKind.Relative);

    private static Uri RecordedGoalsUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/recorded-goals", UriKind.Relative);

    private static Uri RecordedGoalUri(MatchId matchId, GoalId goalId) =>
        new($"/matches/{matchId.Value}/recorded-goals/{goalId.Value}", UriKind.Relative);

    private static Uri RecordedSubstitutionsUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/recorded-substitutions", UriKind.Relative);

    private static Uri RecordedDisciplinaryEventsUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/recorded-disciplinary-events", UriKind.Relative);

    private static string? GetCode(ProblemDetails? problem) =>
        problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<MatchSeed> SeedScheduledMatchWithRosterAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var standard = SampleRegulations.Standard();
        var regulation = new Regulation(
            standard.EntryRules,
            standard.MatchRules,
            standard.StandingRules,
            new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.Red]));
        var competition = Competition.Create(new CompetitionName("Sheet Host Cup"), regulation, _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var dupont = competition.AddDeclaredMember(home.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var martin = competition.AddDeclaredMember(home.Id, "Martin", DeclaredMemberRole.Player, _clock);
        var rival = competition.AddDeclaredMember(away.Id, "Rival", DeclaredMemberRole.Player, _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Stage 1"), regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();

        return new MatchSeed(match.Id, dupont.Id, martin.Id, rival.Id);
    }

    private async Task<MatchSeed> SeedScheduledMatchWithSheetAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var standard = SampleRegulations.Standard();
        var regulation = new Regulation(
            standard.EntryRules,
            standard.MatchRules,
            standard.StandingRules,
            DisciplinaryRules.None);
        var competition = Competition.Create(new CompetitionName("Scheduled Sheet Cup"), regulation, _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var dupont = competition.AddDeclaredMember(home.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var martin = competition.AddDeclaredMember(home.Id, "Martin", DeclaredMemberRole.Player, _clock);
        var rival = competition.AddDeclaredMember(away.Id, "Rival", DeclaredMemberRole.Player, _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Stage 1"), regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.AddDeclaredParticipation(dupont.Id, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(martin.Id, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(rival.Id, Side.Away, CompositionStatus.Starter, _clock);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();

        return new MatchSeed(match.Id, dupont.Id, martin.Id, rival.Id);
    }

    private async Task<MatchSeed> SeedLiveMatchWithSheetAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var standard = SampleRegulations.Standard();
        var regulation = new Regulation(
            standard.EntryRules,
            standard.MatchRules,
            standard.StandingRules,
            new DisciplinaryRules([DisciplinaryType.Yellow]));
        var competition = Competition.Create(new CompetitionName("Mutation Host Cup"), regulation, _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var dupont = competition.AddDeclaredMember(home.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var martin = competition.AddDeclaredMember(home.Id, "Martin", DeclaredMemberRole.Player, _clock);
        var rival = competition.AddDeclaredMember(away.Id, "Rival", DeclaredMemberRole.Player, _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Stage 1"), regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.AddDeclaredParticipation(dupont.Id, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(martin.Id, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(rival.Id, Side.Away, CompositionStatus.Starter, _clock);
        match.Start(_clock);
        match.RecordGoal(dupont.Id, Side.Home, _clock);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();

        return new MatchSeed(match.Id, dupont.Id, martin.Id, rival.Id);
    }

    private sealed record MatchSeed(MatchId MatchId, MemberId DupontId, MemberId MartinId, MemberId RivalId);
}
