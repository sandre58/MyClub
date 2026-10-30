// -----------------------------------------------------------------------
// <copyright file="ResolutionFreshnessDetectorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
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
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class ResolutionFreshnessDetectorTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 18, 0, 0, TimeSpan.Zero));

    private static QualificationPath OverallPath(int position, StageId destinationId, string? slot)
    {
        var destination = slot is null
            ? QualificationDestination.ForPopulation(destinationId)
            : QualificationDestination.ForSlot(destinationId, slot);
        return new QualificationPath(
            position,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            destination);
    }

    private static StructureResolutionOccupantDto[] Evaluate(
        Competition competition,
        IReadOnlyList<Stage> stages,
        params (Stage Stage, IReadOnlyList<Match> Matches)[] played) =>
        [.. EvaluateByStage(competition, stages, played).SelectMany(pair => pair.Value)];

    private static Dictionary<Guid, IReadOnlyList<StructureResolutionOccupantDto>> EvaluateByStage(
        Competition competition,
        IReadOnlyList<Stage> stages,
        params (Stage Stage, IReadOnlyList<Match> Matches)[] played)
    {
        var summaries = played.ToDictionary(
            pair => pair.Stage.Id,
            pair => (IReadOnlyList<MatchSummaryRow>)StageStartReadinessGuard.ToSummaryRows(pair.Matches));
        return new Dictionary<Guid, IReadOnlyList<StructureResolutionOccupantDto>>(
            ResolutionFreshnessDetector.Evaluate(competition, stages, summaries));
    }

    private static IReadOnlyList<(Guid EntryId, string? Path, string? Fingerprint)> Snapshot(Stage stage) =>
    [
        .. stage.Slots.Select(slot => (
            slot.EntryId?.Value ?? Guid.Empty,
            slot.ResolutionProvenance?.SourcePathId,
            slot.ResolutionProvenance?.UpstreamResultFingerprint)),
        .. stage.CompositionEntries.Select(entry => (
            entry.EntryId.Value,
            entry.ResolutionProvenance?.SourcePathId,
            entry.ResolutionProvenance?.UpstreamResultFingerprint)),
    ];

    [Fact]
    public void Just_after_apply_every_qualification_occupant_is_current_and_unchanged()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(3);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        var before = Snapshot(terminal);

        var occupants = Evaluate(competition, [league, terminal], (league, matches));

        occupants.Should().NotBeEmpty();
        occupants.Should().OnlyContain(occupant =>
            occupant.ProvenanceKind == ResolutionProvenanceKind.Qualification
            && occupant.State == StructureResolutionFreshness.AJour
            && occupant.StalenessCause == null);
        Snapshot(terminal).Should().Equal(before);
    }

    [Fact]
    public void Score_correction_marks_qualifier_stale_for_upstream_result()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        var stored = terminal.FindSlot("Champ")!.EntryId!.Value;
        var decisive = matches.Single();
        decisive.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 3)), _clock);

        var occupants = Evaluate(competition, [league, terminal], (league, matches));

        occupants.Where(occupant => occupant.Target == StructureResolutionOccupantTarget.Slot)
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                EntryId = stored.Value,
                State = StructureResolutionFreshness.Perime,
                StalenessCause = StructureResolutionStalenessCause.ResultatAmont,
            });
        terminal.FindSlot("Champ")!.EntryId.Should().Be(stored);
    }

    [Fact]
    public void Qualification_rule_change_marks_occupant_stale_for_rule_without_moving_it()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        var stored = terminal.FindSlot("Champ")!.EntryId!.Value;
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                OverallPath(2, terminal.Id, "Champ"),
            ]),
            _clock);

        var slot = Evaluate(competition, [league, terminal], (league, matches))
            .Should().ContainSingle(occupant => occupant.Target == StructureResolutionOccupantTarget.Slot)
            .Subject;
        slot.State.Should().Be(StructureResolutionFreshness.Perime);
        slot.StalenessCause.Should().Be(StructureResolutionStalenessCause.RegleQualification);
        slot.EntryId.Should().Be(stored.Value);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(stored);
    }

    [Fact]
    public void Stable_tie_keeps_lower_entry_current_when_lower_rows_change()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(4);
        SetScore(matches, entries[0], entries[1], 1, 1);
        SetScore(matches, entries[0], entries[2], 1, 0);
        SetScore(matches, entries[0], entries[3], 1, 0);
        SetScore(matches, entries[1], entries[2], 1, 0);
        SetScore(matches, entries[1], entries[3], 1, 0);
        SetScore(matches, entries[2], entries[3], 1, 0);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        terminal.FindSlot("Champ")!.EntryId.Should().Be(entries[0]);

        SetScore(matches, entries[2], entries[3], 0, 1);

        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules!);
        standing.EntryAt(1)!.Value.Should().Be(entries[0]);
        Evaluate(competition, [league, terminal], (league, matches))
            .Where(occupant => occupant.ProvenanceKind == ResolutionProvenanceKind.Qualification)
            .Should().OnlyContain(occupant =>
                occupant.State == StructureResolutionFreshness.AJour
                && occupant.EntryId == entries[0].Value);
    }

    [Fact]
    public void Administrative_forfeit_that_flips_the_qualifier_is_stale()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        var winner = entries[0];
        var match = matches.Single();
        var forfeited = ForfeitMatchResult.ForWithdrawnSide(
            match,
            winner,
            league.Regulation.MatchRules.AdministrativeResultPolicy);
        match.CorrectMatchResult(forfeited, _clock);

        Evaluate(competition, [league, terminal], (league, matches))
            .Should().Contain(occupant =>
                occupant.EntryId == winner.Value
                && occupant.State == StructureResolutionFreshness.Perime
                && occupant.StalenessCause == StructureResolutionStalenessCause.ResultatAmont);
    }

    [Fact]
    public void Withdrawing_the_qualified_team_marks_the_stored_occupant_stale()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(3);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        competition.Prepare(_clock);
        competition.Start(_clock);
        WithdrawEntry.Execute(competition, entries[0], [league, terminal], matches, _clock);

        Evaluate(competition, [league, terminal], (league, matches))
            .Should().Contain(occupant =>
                occupant.Target == StructureResolutionOccupantTarget.Slot
                && occupant.EntryId == entries[0].Value
                && occupant.State == StructureResolutionFreshness.Perime
                && occupant.StalenessCause == StructureResolutionStalenessCause.ResultatAmont);
    }

    [Fact]
    public void Missing_position_is_indeterminate_and_does_not_throw()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(3);
        ApplyOverall(league, terminal, entries, matches, position: 3, "Champ");
        competition.Prepare(_clock);
        competition.Start(_clock);

        var act = () => WithdrawEntry.Execute(competition, entries[2], [league, terminal], matches, _clock);
        act.Should().NotThrow();

        Evaluate(competition, [league, terminal], (league, matches))
            .Where(occupant => occupant.ProvenanceKind == ResolutionProvenanceKind.Qualification)
            .Should().OnlyContain(occupant => occupant.State == StructureResolutionFreshness.Indetermine);
    }

    [Fact]
    public void Direct_assignment_stays_current_when_another_occupant_is_stale()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        ApplyOverall(league, terminal, entries, matches, position: 1, "Champ");
        terminal.AddSlot("Direct");
        terminal.AddResolvedPopulationEntry(entries[1], _clock);
        terminal.AssignEntryToSlot("Direct", entries[1]);
        league.ReplaceQualificationRules(
            new QualificationRules([OverallPath(2, terminal.Id, "Champ")]),
            _clock);

        var occupants = Evaluate(competition, [league, terminal], (league, matches));
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Champ"
            && occupant.State == StructureResolutionFreshness.Perime);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Direct"
            && occupant.ProvenanceKind == ResolutionProvenanceKind.Directe
            && occupant.State == StructureResolutionFreshness.AJour
            && occupant.StalenessCause == null);
    }

    [Fact]
    public void Non_verified_provenance_is_exposed_as_non_verified()
    {
        var (competition, league, terminal, entries, matches) = LeagueFeed(2);
        terminal.AddSlot("NV");
        terminal.AddResolvedPopulationEntry(entries[0], _clock);
        terminal.ApplyResolvedEntry("NV", entries[0], _clock);

        Evaluate(competition, [league, terminal], (league, matches))
            .Should().Contain(occupant =>
                occupant.SlotKey == "NV"
                && occupant.State == StructureResolutionFreshness.NonVerifiee
                && occupant.ProvenanceKind == ResolutionProvenanceKind.NonVerifiee);
    }

    [Fact]
    public void Added_population_marks_draw_occupants_stale()
    {
        var competition = CreateCompetition.Execute("Draw pop", _clock);
        var entries = AddTeams(competition, 2);
        var cup = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock).Stage;
        ReplaceStageDrawRules.Execute(cup, new DrawRules(DrawMode.Random), _clock);
        cup.ReplaceAffectationAuthoring(entries, _clock);
        var draw = CreateDraw.Execute(cup, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            cup,
            draw.Id,
            DrawInputsFactory.CreateDefault(cup, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(
            cup,
            draw.Id,
            _clock,
            cup.Slots.Select(slot => slot.SlotKey).ToArray(),
            competitionStages: [cup],
            randomSource: new SeededRandomSource(1));
        PublishDraw.Execute(cup, draw.Id, _clock);
        ApplyDraw.Execute(cup, draw.Id, _clock);

        Evaluate(competition, [cup])
            .Where(occupant => occupant.ProvenanceKind == ResolutionProvenanceKind.Tirage)
            .Should().NotBeEmpty()
            .And.OnlyContain(occupant => occupant.State == StructureResolutionFreshness.AJour);

        cup.AddResolvedPopulationEntry(EntryId.New(), _clock);

        Evaluate(competition, [cup])
            .Where(occupant => occupant.ProvenanceKind == ResolutionProvenanceKind.Tirage)
            .Should().OnlyContain(occupant =>
                occupant.State == StructureResolutionFreshness.Perime
                && occupant.StalenessCause == StructureResolutionStalenessCause.ResultatAmont);
    }

    [Fact]
    public void Three_level_chain_marks_qualification_then_downstream_progression()
    {
        var competition = CreateCompetition.Execute("Chain", _clock);
        var entries = AddTeams(competition, 3);
        var groups = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var group = groups.AddGroup("A", _clock);
        groups.AssignEntryToGroup(group.Id, entries[0]);
        groups.AssignEntryToGroup(group.Id, entries[1]);
        competition.AddStage(groups.Id, _clock);
        var groupMatch = Match.Create(competition.Id, groups.Id, entries[0], entries[1], _clock);
        groupMatch.Start(_clock);
        groupMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        var groupMatches = new List<Match> { groupMatch };
        var standing = CalculateStanding.Execute(
            [entries[0], entries[1]],
            groupMatches,
            groups.Regulation.StandingRules!);

        var cup = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        cup.AddRound("QF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        cup.AddSlot("QF-A");
        cup.AddSlot("QF-B");
        cup.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        competition.AddStage(cup.Id, _clock);
        var ranking = Stage.Create(competition.Id, new StageName("Ranking"), SampleRegulations.Standard(), _clock);
        ranking.AddSlot("R1");
        competition.AddStage(ranking.Id, _clock);

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(cup.Id, "QF-A")),
            ]),
            _clock);
        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [group.Id] = standing },
            [groups, cup, ranking],
            _clock);
        var qualifier = cup.FindSlot("QF-A")!.EntryId!.Value;

        cup.AddResolvedPopulationEntry(entries[2], _clock);
        cup.AssignEntryToSlot("QF-B", entries[2]);
        var fixture = cup.AddFixture(cup.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var cupMatch = Match.Create(competition.Id, cup.Id, qualifier, entries[2], _clock);
        cup.AttachMatch(fixture.Id, cupMatch.Id, legIndex: 1, _clock);
        cupMatch.Start(_clock);
        cupMatch.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        cup.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(ranking.Id, "R1")),
            ]),
            _clock);
        ApplyProgressionOutcome.Execute(cup, fixture.Id, [cupMatch], [groups, cup, ranking], _clock);

        groupMatch.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), _clock);
        var occupants = Evaluate(
            competition,
            [groups, cup, ranking],
            (groups, groupMatches),
            (cup, [cupMatch]));

        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "QF-A"
            && occupant.State == StructureResolutionFreshness.Perime
            && occupant.StalenessCause == StructureResolutionStalenessCause.ResultatAmont);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "R1"
            && occupant.State == StructureResolutionFreshness.Perime
            && occupant.StalenessCause == StructureResolutionStalenessCause.AmontPerime);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "QF-B"
            && occupant.ProvenanceKind == ResolutionProvenanceKind.Directe
            && occupant.State == StructureResolutionFreshness.AJour);
    }

    [Fact]
    public void Mutual_qualification_cycle_returns_without_throwing()
    {
        var competition = CreateCompetition.Execute("Cycle", _clock);
        var entries = AddTeams(competition, 2);
        var alpha = Stage.Create(competition.Id, new StageName("Alpha"), SampleRegulations.Standard(), _clock);
        var beta = Stage.Create(competition.Id, new StageName("Beta"), SampleRegulations.Standard(), _clock);
        alpha.AddMatchday(1, _clock);
        beta.AddMatchday(1, _clock);
        competition.AddStage(alpha.Id, _clock);
        competition.AddStage(beta.Id, _clock);
        var alphaMatches = RoundRobin(alpha, entries);
        var betaMatches = RoundRobin(beta, entries);
        ApplyOverall(alpha, beta, entries, alphaMatches, position: 1, slot: null);
        ApplyOverall(beta, alpha, entries, betaMatches, position: 1, slot: null);
        alpha.ReplaceQualificationRules(
            new QualificationRules([OverallPath(2, beta.Id, slot: null)]),
            _clock);

        var act = () => EvaluateByStage(
            competition,
            [alpha, beta],
            (alpha, alphaMatches),
            (beta, betaMatches));

        var occupants = act.Should().NotThrow().Subject;
        occupants[beta.Id.Value].Should().Contain(occupant =>
            occupant.StalenessCause == StructureResolutionStalenessCause.RegleQualification);
        occupants[alpha.Id.Value].Should().Contain(occupant =>
            occupant.StalenessCause == StructureResolutionStalenessCause.AmontPerime);
    }

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, List<Match> Matches) LeagueFeed(
        int teams)
    {
        var competition = CreateCompetition.Execute($"League {teams} {Guid.NewGuid():N}", _clock);
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
        string? slot)
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

        Array.Sort(ids, (left, right) => left.Value.CompareTo(right.Value));
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
                match.Finish(new MatchResult(ResultType.Played, new Score(entries.Length - home, 0)), _clock);
                matches.Add(match);
            }
        }

        return matches;
    }

    private void SetScore(IReadOnlyList<Match> matches, EntryId home, EntryId away, int homeGoals, int awayGoals)
    {
        var match = matches.Single(candidate =>
            candidate.HomeEntryId.Equals(home) && candidate.AwayEntryId.Equals(away));
        match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
    }
}
