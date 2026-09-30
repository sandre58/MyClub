// -----------------------------------------------------------------------
// <copyright file="GenerateNextRoundTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class GenerateNextRoundTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 26, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_round1_while_running_creates_matchday_and_matches()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 4, roundCount: 3);

        var result = GenerateNextRound.Execute(competition, stage, [], _clock);

        result.AlreadyComplete.Should().BeFalse();
        result.RoundIndex.Should().Be(1);
        result.ByeEntryId.Should().BeNull();
        result.CreatedMatches.Should().HaveCount(2);
        stage.Matchdays.Should().ContainSingle().Which.Number.Should().Be(1);
        stage.Matchdays[0].Fixtures.Should().HaveCount(2);
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Execute_round2_after_finished_round1_is_progressive()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 4, roundCount: 3);
        var round1 = GenerateNextRound.Execute(competition, stage, [], _clock);
        FinishAll(round1.CreatedMatches);

        var allMatches = round1.CreatedMatches.ToList();
        var round2 = GenerateNextRound.Execute(competition, stage, allMatches, _clock);

        round2.RoundIndex.Should().Be(2);
        round2.CreatedMatches.Should().HaveCount(2);
        stage.Matchdays.Should().HaveCount(2);

        foreach (var match in round2.CreatedMatches)
        {
            allMatches.Should().NotContain(prior =>
                (prior.HomeEntryId.Equals(match.HomeEntryId) && prior.AwayEntryId.Equals(match.AwayEntryId))
                || (prior.HomeEntryId.Equals(match.AwayEntryId) && prior.AwayEntryId.Equals(match.HomeEntryId)));
        }
    }

    [Fact]
    public void Execute_rejects_when_previous_round_not_finished()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 4, roundCount: 3);
        var round1 = GenerateNextRound.Execute(competition, stage, [], _clock);

        var act = () => GenerateNextRound.Execute(competition, stage, round1.CreatedMatches, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SwissRoundGenerationFailure);
    }

    [Fact]
    public void Execute_odd_participants_records_bye_without_fixture()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 3, roundCount: 2);

        var result = GenerateNextRound.Execute(competition, stage, [], _clock);

        result.ByeEntryId.Should().NotBeNull();
        result.CreatedMatches.Should().HaveCount(1);
        stage.SwissByeHistory.Should().ContainSingle()
            .Which.Should().Be(new SwissBye(1, result.ByeEntryId.Value));
        stage.Matchdays[0].Fixtures.Should().HaveCount(1);
    }

    [Fact]
    public void Execute_rejects_when_stage_not_running()
    {
        var competition = CreateCompetition.Execute("Swiss", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("S"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(2));
        competition.AddStage(stage.Id, _clock);
        stage.Prepare(_clock);

        var act = () => GenerateNextRound.Execute(competition, stage, [], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SwissRoundGenerationFailure);
    }

    [Fact]
    public void Execute_rejects_when_not_swiss()
    {
        var competition = CreateCompetition.Execute("Champ", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock);
        configured.Stage.Prepare(_clock);
        configured.Stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var act = () => GenerateNextRound.Execute(competition, configured.Stage, [], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SwissRoundGenerationFailure);
    }

    [Fact]
    public void Execute_rejects_when_all_rounds_already_generated()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 2, roundCount: 1);
        var first = GenerateNextRound.Execute(competition, stage, [], _clock);
        FinishAll(first.CreatedMatches);

        var act = () => GenerateNextRound.Execute(competition, stage, first.CreatedMatches, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SwissRoundGenerationFailure);
    }

    [Fact]
    public void Domain_AddMatchday_blocked_while_running_but_swiss_progressive_apis_used_by_uc()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 2, roundCount: 2);
        GenerateNextRound.Execute(competition, stage, [], _clock);

        var blocked = () => stage.AddMatchday(2, _clock);
        blocked.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Running);
        stage.IsSwiss.Should().BeTrue();
    }

    private (Competition Competition, Stage Stage) CreateRunningSwiss(int participantCount, int roundCount)
    {
        var competition = CreateCompetition.Execute("Swiss", _clock);
        for (var i = 0; i < participantCount; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var stage = Stage.Create(competition.Id, new StageName("Swiss"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(roundCount));
        competition.AddStage(stage.Id, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        return (competition, stage);
    }

    private void FinishAll(IReadOnlyList<Match> matches)
    {
        foreach (var match in matches)
        {
            match.Start(_clock);
            match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        }
    }
}
