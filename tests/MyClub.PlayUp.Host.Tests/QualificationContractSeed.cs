// -----------------------------------------------------------------------
// <copyright file="QualificationContractSeed.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Minimal championship → terminal qualification graph for HTTP contract tests.
/// </summary>
internal static class QualificationContractSeed
{
    private static readonly FakeClock Clock = new(new DateTimeOffset(2026, 8, 16, 14, 0, 0, TimeSpan.Zero));

    public static async Task<(Guid LeagueId, Guid TerminalId)> CreateAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Contract Qualif"), SampleRegulations.Standard(), Clock);
        var e1 = competition.AddEntry(TeamId.New(), "A", Clock);
        var e2 = competition.AddEntry(TeamId.New(), "B", Clock);
        var e3 = competition.AddEntry(TeamId.New(), "C", Clock);
        competitions.Add(competition);

        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), Clock);
        var md = league.AddMatchday(1, Clock);
        competition.AddStage(league.Id, Clock);

        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), Clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, Clock);

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(terminal.Id, "Champ"))
            ]),
            Clock);

        var entries = new[] { e1.Id, e2.Id, e3.Id };
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var fixture = league.AddFixture(md.Id, Clock);
                var match = Match.Create(competition.Id, league.Id, entries[i], entries[j], Clock);
                match.Start(Clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), Clock);
                league.AttachMatch(fixture.Id, match.Id, legIndex: 1, Clock);
                matches.Add(match);
            }
        }

        stages.Add(league);
        stages.Add(terminal);
        await unitOfWork.SaveChangesAsync();
        return (league.Id.Value, terminal.Id.Value);
    }
}
