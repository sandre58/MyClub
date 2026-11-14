// -----------------------------------------------------------------------
// <copyright file="NeedsAttentionAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Qualification;
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

        attention.Items.Should().NotContain(item =>
            item.Source == NeedsAttentionAssembler.SourceDrawNoSolution
            || item.Source == NeedsAttentionAssembler.SourceQualificationPending
            || item.Source == NeedsAttentionAssembler.SourceQualificationConflict
            || item.Source == NeedsAttentionAssembler.SourceProgressionPending
            || item.Source == NeedsAttentionAssembler.SourceProgressionConflict
            || item.Source == NeedsAttentionAssembler.SourceInsufficientParticipants);
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
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
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

        var qualificationPending = attention.Items
            .Where(item => item.Source == NeedsAttentionAssembler.SourceQualificationPending)
            .ToArray();
        qualificationPending.Should().HaveCount(2);
        qualificationPending.Should().OnlyContain(item => item.TargetType == "Form");

        champ.AddResolvedPopulationEntry(tops[0], _clock);
        champ.RecordFormPathResolution(
            FormPathResolutionKey.FromQualification(
                source.Id,
                source.Regulation.QualificationRules!.Paths.Single(p => p.Source.GroupId!.Equals(groupA.Id))),
            tops[0]);

        attention = NeedsAttentionAssembler.Assemble(
            competition,
            [source, champ],
            new Dictionary<StageId, IReadOnlyList<Match>> { [source.Id] = matches });

        attention.Items
            .Where(item => item.Source == NeedsAttentionAssembler.SourceQualificationPending)
            .Should()
            .ContainSingle(item => item.TargetType == "Form");
    }

    [Fact]
    public void Assemble_coalesces_qualification_conflict_when_matching_slot_is_perime()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        matches.Single().CorrectMatchResult(
            new MatchResult(ResultType.Played, new Score(0, 2)),
            _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [league, terminal],
            new Dictionary<StageId, IReadOnlyList<Match>> { [league.Id] = matches });

        attention.Items.Should().NotContain(item =>
            item.Source == NeedsAttentionAssembler.SourceQualificationConflict);
        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceResolutionPerime
            && item.Severity == NeedsAttentionAssembler.SeverityWarning
            && item.TargetType == "Slot"
            && item.TargetId == $"{terminal.Id.Value}:Champ"
            && item.Params != null
            && item.Params[NeedsAttentionAssembler.QualificationCoalescedParam] == bool.TrueString);
    }

    [Fact]
    public void Assemble_keeps_qualification_conflict_blocking_for_direct_slot_assignment()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        terminal.AddResolvedPopulationEntry(entries[1], _clock);
        terminal.AssignEntryToSlot("Champ", entries[1]);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [league, terminal],
            new Dictionary<StageId, IReadOnlyList<Match>> { [league.Id] = matches });

        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceQualificationConflict
            && item.Severity == NeedsAttentionAssembler.SeverityBlocking
            && item.TargetType == "Slot"
            && item.TargetId == $"{terminal.Id.Value}:Champ");
        attention.Items.Should().NotContain(item =>
            item.Params != null
            && item.Params.ContainsKey(NeedsAttentionAssembler.QualificationCoalescedParam));
    }

    [Fact]
    public void Assemble_without_slot_occupant_emits_qualification_pending_not_coalesced()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(terminal.Id, "Champ")),
            ]),
            _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [league, terminal],
            new Dictionary<StageId, IReadOnlyList<Match>> { [league.Id] = matches });

        attention.Items.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceQualificationPending);
        attention.Items.Should().NotContain(item =>
            item.Params != null
            && item.Params.ContainsKey(NeedsAttentionAssembler.QualificationCoalescedParam));
    }

    [Fact]
    public void Assemble_prog_group_pending_until_group_membership()
    {
        var competition = Competition.Create(new CompetitionName("ProgGroup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var cup = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        cup.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        cup.AddSlot("KO-A");
        cup.AddSlot("KO-B");
        cup.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        competition.AddStage(cup.Id, _clock);
        var groups = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = groups.AddGroup("A", _clock);
        competition.AddStage(groups.Id, _clock);

        var fixture = cup.AddFixture(cup.Rounds[0].Id, _clock, "KO-A", "KO-B", "P1");
        cup.ApplyResolvedEntry("KO-A", home.Id, _clock);
        cup.ApplyResolvedEntry("KO-B", away.Id, _clock);
        var match = Match.Create(competition.Id, cup.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        cup.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        cup.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForGroup(groups.Id, groupA.Id))
            ]),
            _clock);

        var attention = NeedsAttentionAssembler.Assemble(
            competition,
            [cup, groups],
            new Dictionary<StageId, IReadOnlyList<Match>> { [cup.Id] = [match] });

        attention.Items
            .Where(item => item.Source == NeedsAttentionAssembler.SourceProgressionPending)
            .Should()
            .ContainSingle(item => item.TargetType == "Group");

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
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.AddSlot("SF1-A");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        competition.AddStage(stage.Id, _clock);

        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "KO-A", "KO-B", "P1");
        stage.ApplyResolvedEntry("KO-A", home.Id, _clock);
        stage.ApplyResolvedEntry("KO-B", away.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1",
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

    private static QualificationPath OverallPath(int position, StageId destinationId, string slot) =>
        new(
            position,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForSlot(destinationId, slot));

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, List<Match> Matches)
        LeagueFeed(int teams)
    {
        var competition = CreateCompetition.Execute($"League {teams}", _clock);
        var entries = AddTeams(competition, teams);
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);
        return (competition, league, terminal, entries, RoundRobin(league, entries));
    }

    private void ApplyOverall(
        Stage source,
        Stage destination,
        EntryId[] entries,
        IReadOnlyList<Match> matches,
        int position,
        string slot)
    {
        var standing = CalculateStanding.Execute(entries, matches, source.Regulation.StandingRules!);
        source.ReplaceQualificationRules(
            new QualificationRules([OverallPath(position, destination.Id, slot)]),
            _clock);
        ApplyQualification.Execute(source, standing, [source, destination], _clock);
    }

    private EntryId[] AddTeams(Competition competition, int count)
    {
        var ids = new EntryId[count];
        for (var index = 0; index < count; index++)
        {
            ids[index] = AddEntry.Execute(competition, $"T{index}", _clock).Id;
        }

        return ids;
    }

    private List<Match> RoundRobin(Stage stage, EntryId[] entries)
    {
        var matches = new List<Match>();
        for (var home = 0; home < entries.Length; home++)
        {
            for (var away = home + 1; away < entries.Length; away++)
            {
                var match = Match.Create(stage.CompetitionId, stage.Id, entries[home], entries[away], _clock);
                match.Start(_clock);
                match.Finish(
                    new MatchResult(ResultType.Played, new Score(entries.Length - home, 0)),
                    _clock);
                matches.Add(match);
            }
        }

        return matches;
    }
}
