// -----------------------------------------------------------------------
// <copyright file="ConsultationAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class ConsultationAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_draft_without_structure_has_empty_sections()
    {
        var competition = Competition.Create(new CompetitionName("Draft"), SampleRegulations.Standard(), _clock);

        var view = ConsultationAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.Status.Should().Be(CompetitionStatus.Draft);
        view.Results.Should().BeEmpty();
        view.Standings.Applicable.Should().BeFalse();
        view.Standings.NotApplicableReason.Should().Be(ConsultationAssembler.NotApplicableNoStructure);
        view.Structure.Stages.Should().BeEmpty();
    }

    [Fact]
    public void Assemble_championship_exposes_results_standings_and_matchdays()
    {
        var ctx = CreateFinishedChampionship();

        var view = ConsultationAssembler.Assemble(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = ctx.Matches });

        view.FormatKind.Should().Be(StructureFormatKind.Championship);
        view.Results.Should().HaveCount(3);
        view.Results.Should().OnlyContain(result => result.Status == MatchStatus.Finished && result.Score != null);
        view.Results.Should().OnlyContain(result => result.MatchId != Guid.Empty);
        view.Results.Should().OnlyContain(result =>
            result.MatchdayNumber == 1 && result.ContextLabel == "Journée 1");
        view.Results.Should().OnlyContain(result => result.ResultType == ResultType.Played);
        view.Standings.Applicable.Should().BeTrue();
        view.Standings.Tables.Should().ContainSingle(table => table.Scope == ConsultationAssembler.ScopeOverall);
        view.Standings.Tables[0].Rows.Should().HaveCount(3);
        view.Structure.Stages.Should().ContainSingle();
        view.Structure.Stages[0].Matchdays.Should().ContainSingle(md => md.Number == 1);
        view.Structure.Stages[0].Matchdays[0].Fixtures.Should().HaveCount(3);
    }

    [Fact]
    public void Assemble_cup_standings_not_applicable()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A", _clock);
        competition.AddStage(stage.Id, _clock);

        var view = ConsultationAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.FormatKind.Should().Be(StructureFormatKind.Cup);
        view.Standings.Applicable.Should().BeFalse();
        view.Standings.NotApplicableReason.Should().Be(ConsultationAssembler.NotApplicableCupFormat);
        view.Structure.Stages[0].Rounds.Should().ContainSingle();
        view.Structure.Stages[0].Slots.Should().ContainSingle(slot => slot.SlotKey == "SF1-A");
    }

    [Fact]
    public void Assemble_groups_produces_per_group_tables()
    {
        var competition = Competition.Create(new CompetitionName("Groups"), SampleRegulations.Standard(), _clock);
        var a = competition.AddEntry(TeamId.New(), "A", _clock);
        var b = competition.AddEntry(TeamId.New(), "B", _clock);
        var c = competition.AddEntry(TeamId.New(), "C", _clock);
        var d = competition.AddEntry(TeamId.New(), "D", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var g1 = stage.AddGroup("G1", _clock);
        stage.AssignEntryToGroup(g1.Id, a.Id);
        stage.AssignEntryToGroup(g1.Id, b.Id);
        var g2 = stage.AddGroup("G2", _clock);
        stage.AssignEntryToGroup(g2.Id, c.Id);
        stage.AssignEntryToGroup(g2.Id, d.Id);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var md = stage.AddMatchday(1, _clock);
        var f1 = stage.AddFixture(md.Id, _clock);
        var m1 = Match.Create(competition.Id, stage.Id, a.Id, b.Id, _clock);
        m1.Start(_clock);
        m1.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        stage.AttachMatch(f1.Id, m1.Id, legIndex: 1, _clock);

        var view = ConsultationAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [m1] });

        view.FormatKind.Should().Be(StructureFormatKind.Groups);
        view.Standings.Applicable.Should().BeTrue();
        view.Standings.Tables.Should().HaveCount(2);
        view.Standings.Tables.Should().OnlyContain(table => table.Scope == ConsultationAssembler.ScopeGroup);
        view.Results.Should().ContainSingle();
        view.Structure.Stages[0].Groups.Should().HaveCount(2);
    }

    [Fact]
    public void Assemble_scheduled_matches_are_not_results()
    {
        var competition = Competition.Create(new CompetitionName("Open"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);

        var view = ConsultationAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        view.Results.Should().BeEmpty();
        view.Standings.Applicable.Should().BeTrue();
    }

    [Fact]
    public void Assemble_completed_and_archived_remain_readable()
    {
        var ctx = CreateFinishedChampionship();
        ctx.Competition.Complete(CompletionMode.Normal, _clock);

        var completed = ConsultationAssembler.Assemble(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = ctx.Matches });
        completed.Status.Should().Be(CompetitionStatus.Completed);
        completed.CompletionMode.Should().Be(CompletionMode.Normal);
        completed.Results.Should().NotBeEmpty();

        ctx.Competition.Archive(_clock);
        var archived = ConsultationAssembler.Assemble(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = ctx.Matches });
        archived.Status.Should().Be(CompetitionStatus.Archived);
        archived.Results.Should().HaveCount(completed.Results.Count);
    }

    [Fact]
    public void Assemble_does_not_mutate_aggregates()
    {
        var ctx = CreateFinishedChampionship();
        var statusBefore = ctx.Competition.Status;
        var matchStatusBefore = ctx.Matches[0].Status;

        _ = ConsultationAssembler.Assemble(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = ctx.Matches });

        ctx.Competition.Status.Should().Be(statusBefore);
        ctx.Matches[0].Status.Should().Be(matchStatusBefore);
    }

    private (Competition Competition, Stage Stage, IReadOnlyList<Match> Matches) CreateFinishedChampionship()
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
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
                stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
                matches.Add(match);
            }
        }

        return (competition, stage, matches);
    }
}
