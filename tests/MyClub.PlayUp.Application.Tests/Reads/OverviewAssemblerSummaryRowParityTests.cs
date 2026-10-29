// -----------------------------------------------------------------------
// <copyright file="OverviewAssemblerSummaryRowParityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Placement;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Overview load-shape parity — full <see cref="Match"/> adapter vs <see cref="MatchSummaryRow"/> path.
/// </summary>
public sealed class OverviewAssemblerSummaryRowParityTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_summary_rows_matches_match_adapter_on_championship_with_match_sheets()
    {
        var (competition, stage, matches) = CreateRunningChampionshipWithSheets();
        AssertParity(competition, stage, matches);
    }

    [Fact]
    public void Assemble_summary_rows_matches_match_adapter_on_cup_penalty_decider()
    {
        var (competition, stage, matches) = CreateRunningCupWithPenaltyDecider();
        AssertParity(competition, stage, matches);
    }

    private (Competition Competition, Stage Stage, IReadOnlyList<Match> Matches) CreateRunningChampionshipWithSheets()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var e1 = competition.AddEntry(TeamId.New(), "A", _clock);
        var e2 = competition.AddEntry(TeamId.New(), "B", _clock);
        var e3 = competition.AddEntry(TeamId.New(), "C", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var md = stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var entries = new[] { e1.Id, e2.Id, e3.Id };
        var matches = new List<Match>();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var fixture = stage.AddFixture(md.Id, _clock);
                var match = Match.Create(competition.Id, stage.Id, entries[i], entries[j], _clock);
                match.AddDeclaredParticipation(MemberId.New(), Side.Home, CompositionStatus.Starter, _clock, 9);
                match.AddDeclaredParticipation(MemberId.New(), Side.Away, CompositionStatus.Starter, _clock, 10);
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
                stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
                matches.Add(match);
            }
        }

        return (competition, stage, matches);
    }

    private (Competition Competition, Stage Stage, IReadOnlyList<Match> Matches) CreateRunningCupWithPenaltyDecider()
    {
        var competition = Competition.Create(new CompetitionName("Cup final"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha", _clock).Id;
        var away = competition.AddEntry(TeamId.New(), "Beta", _clock).Id;

        var stage = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound(
            "Final",
            new TieFormat(
                TieFormat.SingleLeg,
                aggregateScoring: false,
                extraTimeRule: new ExtraTimeRule(),
                penaltyShootoutRule: new PenaltyShootoutRule()),
            _clock);
        stage.AddSlot("F-A");
        stage.AddSlot("F-B");
        stage.ReplaceBracketPairs([new BracketPair("F1", "F-A", "F-B")]);
        competition.AddStage(stage.Id, _clock);

        var fixture = stage.AddFixture(round.Id, _clock, "F-A", "F-B", "F1");
        stage.ApplyResolvedEntry("F-A", home, _clock);
        stage.ApplyResolvedEntry("F-B", away, _clock);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath("F1", ProgressionOutcome.Winner, 1),
                new PlacementAwardPath("F1", ProgressionOutcome.Loser, 2)
            ]),
            _clock);

        var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.Prepare(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        stage.Start(_clock);

        match.Start(_clock);
        match.Finish(
            new MatchResult(
                ResultType.Played,
                new Score(0, 0),
                extraTimePlayed: true,
                penaltyShootoutScore: new PenaltyShootoutScore(5, 4)),
            _clock);

        return (competition, stage, [match]);
    }

    private static void AssertParity(Competition competition, Stage stage, IReadOnlyList<Match> matches)
    {
        var byMatch = new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = matches };
        var bySummary = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = StageStartReadinessGuard.ToSummaryRows([.. matches])
        };

        var fromMatches = OverviewAssembler.Assemble(competition, [stage], byMatch);
        var fromSummaryRows = OverviewAssembler.Assemble(competition, [stage], bySummary);

        fromSummaryRows.Should().BeEquivalentTo(fromMatches);
    }
}
