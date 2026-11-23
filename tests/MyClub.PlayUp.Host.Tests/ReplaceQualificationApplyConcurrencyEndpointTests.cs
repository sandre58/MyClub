// -----------------------------------------------------------------------
// <copyright file="ReplaceQualificationApplyConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-REST-5 — ReplaceStageQualificationIntents versus ApplyQualification under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReplaceQualificationApplyConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 20, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task ReplaceQualificationIntents_then_applyQualification_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedTwoTeamQualificationAsync(factory);
            var replaceBody = PositionTwoReplaceRequest(seed.TerminalId);
            var (replaceResponse, applyResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => PutQualificationRulesAsync(client, seed.LeagueId, replaceBody),
                    () => client.PostAsync(QualificationApplyUri(seed.LeagueId), content: null));

            using (replaceResponse)
            using (applyResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(replaceResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(applyResponse);

                replaceResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                applyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                var league = await LoadLeagueAsync(factory, seed.LeagueId);
                league!.Regulation.QualificationRules!.Paths.Should().ContainSingle()
                    .Which.Selection.Value.Should().Be(2);

                var applyBody =
                    await applyResponse.Content.ReadFromJsonAsync<QualificationApplyResponse>(HostJson.Options);
                applyBody.Should().NotBeNull();
                applyBody!.Assignments.Should().ContainSingle()
                    .Which.EntryId.Should().Be(seed.BetaEntryId.Value);

                var terminal = await LoadTerminalAsync(factory, seed.TerminalId);
                terminal!.CompositionEntries.Should().ContainSingle()
                    .Which.EntryId.Should().Be(seed.BetaEntryId);
            }
        }
    }

    [IntegrationFact]
    public async Task ApplyQualification_then_replaceQualificationIntents_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedTwoTeamQualificationAsync(factory);
            var replaceBody = PositionTwoReplaceRequest(seed.TerminalId);
            var (applyResponse, replaceResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(QualificationApplyUri(seed.LeagueId), content: null),
                    () => PutQualificationRulesAsync(client, seed.LeagueId, replaceBody));

            using (applyResponse)
            using (replaceResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(applyResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(replaceResponse);

                applyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                replaceResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                var applyBody =
                    await applyResponse.Content.ReadFromJsonAsync<QualificationApplyResponse>(HostJson.Options);
                applyBody.Should().NotBeNull();
                applyBody!.Assignments.Should().ContainSingle()
                    .Which.EntryId.Should().Be(seed.AlphaEntryId.Value);

                var league = await LoadLeagueAsync(factory, seed.LeagueId);
                league!.Regulation.QualificationRules!.Paths.Should().ContainSingle()
                    .Which.Selection.Value.Should().Be(2);

                var terminal = await LoadTerminalAsync(factory, seed.TerminalId);
                terminal!.CompositionEntries.Should().ContainSingle()
                    .Which.EntryId.Should().Be(seed.AlphaEntryId);
            }
        }
    }

    private static string QualificationApplyUri(StageId leagueId) =>
        $"/stages/{leagueId.Value}/qualification/apply";

    private static Task<HttpResponseMessage> PutQualificationRulesAsync(
        HttpClient client,
        StageId leagueId,
        ReplaceStageQualificationRulesRequest body) =>
        client.PutAsJsonAsync($"/stages/{leagueId.Value}/qualification-rules", body, HostJson.Options);

    private static ReplaceStageQualificationRulesRequest PositionTwoReplaceRequest(StageId terminalId) =>
        new(
        [
            new QualificationIntentRequest(
                Guid.NewGuid(),
                1,
                QualificationIntentSourceKind.Overall,
                2,
                2,
                terminalId.Value,
                DestinationSlotKeys: ["Champ"])
        ]);

    private async Task<Seed> SeedTwoTeamQualificationAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName($"REST5 overlap {Guid.NewGuid():N}"),
            SampleRegulations.Standard(),
            _clock);
        var alpha = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var beta = competition.AddEntry(TeamId.New(), "Beta", _clock);
        competitions.Add(competition);

        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var matchday = league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);

        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(terminal.Id, "Champ"))
            ]),
            _clock);

        var addFixture = league.AddFixture(matchday.Id, _clock);
        var match = Match.Create(competition.Id, league.Id, alpha.Id, beta.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        league.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
        matches.Add(match);

        stages.Add(league);
        stages.Add(terminal);
        await unitOfWork.SaveChangesAsync();

        return new Seed(competition.Id, league.Id, terminal.Id, alpha.Id, beta.Id);
    }

    private static async Task<Stage?> LoadLeagueAsync(PlayUpWebApplicationFactory factory, StageId leagueId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(leagueId);
    }

    private static async Task<Stage?> LoadTerminalAsync(PlayUpWebApplicationFactory factory, StageId terminalId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(terminalId);
    }

    private sealed record Seed(
        CompetitionId CompetitionId,
        StageId LeagueId,
        StageId TerminalId,
        EntryId AlphaEntryId,
        EntryId BetaEntryId);
}
