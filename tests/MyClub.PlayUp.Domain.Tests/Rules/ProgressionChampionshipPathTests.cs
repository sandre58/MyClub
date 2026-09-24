// -----------------------------------------------------------------------
// <copyright file="ProgressionChampionshipPathTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class ProgressionChampionshipPathTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void TerminalRound_single_round_with_fixtures_is_terminal()
    {
        var stage = CreateCupWithRoundFixtures([4]);

        var terminal = ProgressionChampionshipPath.TerminalRound(stage.Rounds);

        terminal.Should().Be(stage.Rounds[0]);
    }

    [Fact]
    public void TerminalRound_classic_ko_prefix_ends_at_finale_before_third_place()
    {
        var stage = CreateCupWithRoundFixtures([4, 2, 1, 1]);

        var terminal = ProgressionChampionshipPath.TerminalRound(stage.Rounds);

        terminal.Should().Be(stage.Rounds[2]);
        ProgressionChampionshipPath.IsChampionshipTerminal(stage.Rounds, stage.Rounds[2].Id)
            .Should().BeTrue();
        ProgressionChampionshipPath.IsChampionshipTerminal(stage.Rounds, stage.Rounds[3].Id)
            .Should().BeFalse();
        ProgressionChampionshipPath.IsChampionshipTerminal(stage.Rounds, stage.Rounds[0].Id)
            .Should().BeFalse();
    }

    [Fact]
    public void TerminalRound_halving_chain_without_side_bracket()
    {
        var stage = CreateCupWithRoundFixtures([8, 4, 2, 1]);

        ProgressionChampionshipPath.TerminalRound(stage.Rounds).Should().Be(stage.Rounds[^1]);
    }

    [Fact]
    public void TerminalRound_skips_rounds_without_fixtures()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("Empty", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var playable = stage.AddRound("Final", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddFixture(playable.Id, _clock);

        ProgressionChampionshipPath.TerminalRound(stage.Rounds).Should().Be(playable);
    }

    [Fact]
    public void Materialize_rejects_Winner_on_intermediate_round()
    {
        var stage = CreateCupWithRoundFixtures([2, 1]);
        var peer = StageId.New();
        var intent = new ProgressionIntent(
            IntentId.New(),
            1,
            stage.Rounds[0].Id,
            ProgressionOutcome.Winner,
            peer);

        var act = () => ProgressionPathExpander.Materialize([intent], stage.Rounds, stage.BracketPairs);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    [Fact]
    public void Materialize_allows_Winner_on_championship_terminal()
    {
        var stage = CreateCupWithRoundFixtures([2, 1]);
        var peer = StageId.New();
        var intent = new ProgressionIntent(
            IntentId.New(),
            1,
            stage.Rounds[1].Id,
            ProgressionOutcome.Winner,
            peer);

        var paths = ProgressionPathExpander.Materialize([intent], stage.Rounds, stage.BracketPairs);

        paths.Should().HaveCount(1);
    }

    [Fact]
    public void Materialize_allows_Loser_on_intermediate_round()
    {
        var stage = CreateCupWithRoundFixtures([2, 1]);
        var peer = StageId.New();
        var intent = new ProgressionIntent(
            IntentId.New(),
            1,
            stage.Rounds[0].Id,
            ProgressionOutcome.Loser,
            peer);

        var paths = ProgressionPathExpander.Materialize([intent], stage.Rounds, stage.BracketPairs);

        paths.Should().HaveCount(2);
    }

    [Fact]
    public void ReplaceProgressionRules_rejects_Winner_path_from_intermediate_round()
    {
        var stage = CreateCupWithRoundFixtures([2, 1]);
        var peer = StageId.New();
        var fixture = stage.Rounds[0].Fixtures[0];

        var act = () => stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fixture.Id.Value.ToString("N"),
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(peer))
            ]),
            _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
    }

    private Stage CreateCupWithRoundFixtures(int[] fixtureCountsPerRound)
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        for (var r = 0; r < fixtureCountsPerRound.Length; r++)
        {
            var round = stage.AddRound(
                $"R{r + 1}",
                new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
                _clock);
            for (var i = 0; i < fixtureCountsPerRound[r]; i++)
            {
                stage.AddFixture(round.Id, _clock);
            }
        }

        return stage;
    }
}
