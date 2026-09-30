// -----------------------------------------------------------------------
// <copyright file="QualificationContractSeed.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.TestKit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Minimal championship → terminal qualification graph for HTTP contract tests (TestKit).
/// </summary>
internal static class QualificationContractSeed
{
    private static readonly FakeClock Clock = new(new DateTimeOffset(2026, 8, 16, 14, 0, 0, TimeSpan.Zero));

    public static async Task<(Guid LeagueId, Guid TerminalId)> CreateAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Contract Qualif", Clock)
            .WithTeams("A", "B", "C");

        var league = situation.AddBareStage("League");
        var md = league.AddMatchday(1, Clock);

        var terminal = situation.AddBareStage("Terminal");
        terminal.AddSlot("Champ");

        situation.WithQualificationPaths(
            league,
            new QualificationPathSpec(
                1,
                SelectionMode.Position,
                1,
                terminal.Id.Value,
                RankingScope.Overall));

        var entries = situation.Competition.Entries.Select(e => e.Id).ToArray();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var fixture = league.AddFixture(md.Id, Clock);
                var match = Match.Create(
                    situation.Competition.Id,
                    league.Id,
                    entries[i],
                    entries[j],
                    Clock);
                match.Start(Clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), Clock);
                league.AttachMatch(fixture.Id, match.Id, legIndex: 1, Clock);
                situation.TrackMatch(match);
            }
        }

        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return (league.Id.Value, terminal.Id.Value);
    }
}
