// -----------------------------------------------------------------------
// <copyright file="NeedsAttentionAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class NeedsAttentionAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_when_finished_match_without_consequence_rules_returns_empty()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        attention.Count.Should().Be(0);
    }

    [Fact]
    public void Assemble_detects_progression_pending_when_slot_empty()
    {
        var ctx = CreateFinishedKnockoutWithProgression();

        var attention = NeedsAttentionAssembler.Assemble(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceProgressionPending);
    }

    [Fact]
    public void Assemble_detects_progression_conflict_when_slot_has_other_entry()
    {
        var ctx = CreateFinishedKnockoutWithProgression();
        ctx.Stage.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceProgressionConflict);
    }

    [Fact]
    public void Assemble_detects_draw_no_solution()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.MarkDrawNoSolution(draw.Id, _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceDrawNoSolution);
    }

    [Fact]
    public void Assemble_reports_insufficient_participants_in_draft_when_below_minimum()
    {
        var competition = Competition.Create(new CompetitionName("Thin"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Only", _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceInsufficientParticipants
            && item.Severity == NeedsAttentionAssembler.SeverityBlocking
            && item.TargetType == "Competition"
            && item.TargetId == competition.Id.Value.ToString()
            && item.Params != null
            && item.Params["activeCount"] == "1"
            && item.Params["minimumTeams"] == "2"
            && item.Params["missingCount"] == "1");
    }

    [Fact]
    public void Assemble_does_not_report_insufficient_participants_when_at_minimum()
    {
        var competition = Competition.Create(new CompetitionName("Full"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        attention.Items.Should().NotContain(item =>
            item.Source == NeedsAttentionAssembler.SourceInsufficientParticipants);
    }

    [Fact]
    public void Assemble_does_not_report_insufficient_participants_after_start()
    {
        var competition = Competition.Create(new CompetitionName("Running thin"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Only", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        attention.Items.Should().NotContain(item =>
            item.Source == NeedsAttentionAssembler.SourceInsufficientParticipants);
    }

    [Fact]
    public void Assemble_qual_form_pending_until_provenance_recorded()
    {
        var competition = Competition.Create(new CompetitionName("FormAtt"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, tops, champ) = CreateGroupsToChampForm(competition);

        var matches = BuildGroupMatches(competition, source, groupA, groupB);
        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [source, champ],
            new Dictionary<StageId, IReadOnlyList<Match>> { [source.Id] = matches });

        attention.Items.Should().HaveCount(2);
        attention.Items.Should().OnlyContain(item =>
            item.Source == NeedsAttentionAssembler.SourceQualificationPending
            && item.TargetType == "Form");

        champ.AddResolvedPopulationEntry(tops[0], _clock);
        champ.RecordFormPathResolution(
            FormPathResolutionKey.FromQualification(
                source.Id,
                source.Regulation.QualificationRules!.Paths.Single(p => p.Source.GroupId!.Equals(groupA.Id))),
            tops[0],
            _clock);

        attention = NeedsAttentionAssembler.Assemble(
            competition,
            [source, champ],
            new Dictionary<StageId, IReadOnlyList<Match>> { [source.Id] = matches });

        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceQualificationPending
            && item.TargetType == "Form");
    }

    [Fact]
    public void Assemble_prog_group_pending_until_group_membership()
    {
        var competition = Competition.Create(new CompetitionName("ProgGroup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var cup = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        cup.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        competition.AddStage(cup.Id, _clock);
        var groups = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = groups.AddGroup("A", _clock);
        competition.AddStage(groups.Id, _clock);

        var fixture = cup.AddFixture(cup.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, cup.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        cup.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        cup.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForGroup(groups.Id, groupA.Id))
            ]),
            _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [cup, groups],
            new Dictionary<StageId, IReadOnlyList<Match>> { [cup.Id] = [match] });

        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceProgressionPending
            && item.TargetType == "Group");

        groups.AddResolvedPopulationEntry(home.Id, _clock);
        groups.ApplyResolvedGroupEntry(groupA.Id, home.Id);

        attention = NeedsAttentionAssembler.Assemble(
            competition,
            [cup, groups],
            new Dictionary<StageId, IReadOnlyList<Match>> { [cup.Id] = [match] });

        attention.Items.Should().NotContain(item =>
            item.Source == NeedsAttentionAssembler.SourceProgressionPending);
    }

    private (Competition Competition, Stage Stage, Match Match) CreateFinishedKnockoutWithProgression()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A");
        competition.AddStage(stage.Id, _clock);

        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        return (competition, stage, match);
    }

    private (Stage Source, Group GroupA, Group GroupB, EntryId[] Tops, Stage Champ)
        CreateGroupsToChampForm(Competition competition)
    {
        var source = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = source.AddGroup("A", _clock);
        var groupB = source.AddGroup("B", _clock);
        var eA1 = competition.AddEntry(TeamId.New(), "A1", _clock).Id;
        var eA2 = competition.AddEntry(TeamId.New(), "A2", _clock).Id;
        var eB1 = competition.AddEntry(TeamId.New(), "B1", _clock).Id;
        var eB2 = competition.AddEntry(TeamId.New(), "B2", _clock).Id;
        source.AssignEntryToGroup(groupA.Id, eA1);
        source.AssignEntryToGroup(groupA.Id, eA2);
        source.AssignEntryToGroup(groupB.Id, eB1);
        source.AssignEntryToGroup(groupB.Id, eB2);
        source.AddMatchday(1, _clock);
        competition.AddStage(source.Id, _clock);

        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(champ.Id, _clock);

        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            champ.Id,
            destinationForm: true);
        source.ReplaceQualificationRules(
            QualificationRules.FromIntents([intent], [groupA.Id, groupB.Id]),
            _clock);

        return (source, groupA, groupB, [eA1, eB1], champ);
    }

    private IReadOnlyList<Match> BuildGroupMatches(
        Competition competition,
        Stage source,
        Group groupA,
        Group groupB)
    {
        var a1 = groupA.EntryIds[0];
        var a2 = groupA.EntryIds[1];
        var b1 = groupB.EntryIds[0];
        var b2 = groupB.EntryIds[1];
        var mA = Match.Create(competition.Id, source.Id, a1, a2, _clock);
        mA.Start(_clock);
        mA.Finish(new MatchResult(ResultType.Played, new Score(3, 0)), _clock);
        var mB = Match.Create(competition.Id, source.Id, b1, b2, _clock);
        mB.Start(_clock);
        mB.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        return [mA, mB];
    }
}
