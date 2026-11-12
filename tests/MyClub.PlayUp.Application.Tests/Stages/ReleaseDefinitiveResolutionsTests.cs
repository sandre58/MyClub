// -----------------------------------------------------------------------
// <copyright file="ReleaseDefinitiveResolutionsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages.ReleaseResolution;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using ApplicationErrorCodes = MyClub.PlayUp.Application.ApplicationErrorCodes;
using ApplicationFailureException = MyClub.PlayUp.Application.ApplicationFailureException;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ReleaseDefinitiveResolutionsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void A_cancelled_match_excludes_one_place_and_releases_the_other()
    {
        var (competition, league, terminal, entries, match) = League();
        var third = AddEntry.Execute(competition, "Est", _clock).Id;
        Stamp(league, terminal, ("A", entries[0]), ("B", entries[1]));
        var cancelled = Match.Create(competition.Id, terminal.Id, entries[1], third, _clock);
        cancelled.Cancel(_clock);
        Complete(league);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match, cancelled };

        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);

        terminal.FindSlot("A")!.EntryId.Should().Be(entries[0]);
        Primary(planned).Should().ContainSingle(row => row.SlotKey == "A" && row.EntryName == "Nord");
        planned.Exclusions.Should().ContainSingle(row =>
            row.SlotKey == "B" && row.Code == ApplicationErrorCodes.ResolutionReleaseBlockedByCancelledMatch);
        planned.Releases.Should().Contain(row => row.SideEffect && row.Target == StructureResolutionOccupantTarget.Population);

        ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, Places(planned), stages, matches, _clock);

        terminal.FindSlot("A")!.EntryId.Should().BeNull();
        terminal.FindSlot("B")!.EntryId.Should().Be(entries[1]);
        cancelled.HomeEntryId.Should().Be(entries[1]);
    }

    [Fact]
    public void An_exception_on_one_place_becomes_an_exclusion()
    {
        var (competition, league, good, entries, match) = League();
        var bad = Stage.Create(competition.Id, new StageName("Coupe"), SampleRegulations.Standard(), _clock);
        competition.AddStage(bad.Id, _clock);
        var est = AddEntry.Execute(competition, "Est", _clock).Id;
        var ouest = AddEntry.Execute(competition, "Ouest", _clock).Id;
        Stamp(league, ("Keep", good, entries[1]), ("Bad", bad, entries[0]));
        bad.AddSlot("Mate");
        bad.AddRound("Finale", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        bad.ReplaceBracketPairs([new BracketPair("P1", "Bad", "Mate")]);
        var fixture = bad.AddFixture(bad.Rounds[0].Id, _clock, "Bad", "Mate", "P1");
        var mismatched = Match.Create(competition.Id, bad.Id, est, ouest, _clock);
        bad.AttachMatch(fixture.Id, mismatched.Id, 1, _clock);
        Complete(league);
        var stages = new List<Stage> { league, good, bad };
        var matches = new List<Match> { match, mismatched };

        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);

        Primary(planned).Should().ContainSingle(row => row.SlotKey == "Keep");
        planned.Exclusions.Should().ContainSingle(row =>
            row.SlotKey == "Bad" && row.Code == StageErrorCodes.MaterializedOpponentConflict);
        bad.FindSlot("Bad")!.EntryId.Should().Be(entries[0]);
    }

    [Fact]
    public void Two_linked_places_are_released_and_the_population_leaves_with_the_last_one()
    {
        var (competition, league, terminal, entries, match) = League();
        terminal.AddSlot("Lien");
        var group = terminal.AddGroup("A", _clock);
        var slot = Path(1, QualificationDestination.ForSlot(terminal.Id, "Lien"));
        var grouped = Path(2, QualificationDestination.ForGroup(terminal.Id, group.Id));
        league.ReplaceQualificationRules(new QualificationRules([slot, grouped]), _clock);
        var slotProvenance = Provenance(league, slot);
        var groupProvenance = Provenance(league, grouped);
        terminal.AddResolvedPopulationEntry(entries[0], _clock, slotProvenance);
        terminal.ApplyResolvedEntry("Lien", entries[0], _clock, slotProvenance);
        terminal.ApplyResolvedGroupEntry(group.Id, entries[0], groupProvenance);
        Complete(league);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };

        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);

        var primary = Primary(planned);
        primary.Should().HaveCount(2);
        primary[0].Target.Should().Be(StructureResolutionOccupantTarget.Slot);
        primary[0].PopulationRemoved.Should().BeFalse();
        primary[1].Target.Should().Be(StructureResolutionOccupantTarget.Group);
        primary[1].PopulationRemoved.Should().BeTrue();
        planned.Releases.Should().Contain(row =>
            row.SideEffect && row.Target == StructureResolutionOccupantTarget.Population && row.EntryName == "Nord");
        terminal.FindSlot("Lien")!.EntryId.Should().Be(entries[0]);

        ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, Places(planned), stages, matches, _clock);

        terminal.FindSlot("Lien")!.EntryId.Should().BeNull();
        group.EntryIds.Should().BeEmpty();
        terminal.CompositionEntries.Should().NotContain(entry => entry.EntryId.Equals(entries[0]));
    }

    [Fact]
    public void A_draw_a_running_match_and_a_referenced_population_are_exclusions()
    {
        var draw = DrawWorld();
        var running = RunningWorld();
        var referenced = ReferencedWorld();

        draw.Plan.Exclusions.Should().ContainSingle(row =>
            row.Code == ApplicationErrorCodes.ResolutionReleaseBlockedByDraw);
        draw.Plan.Releases.Should().BeEmpty();

        running.Plan.Exclusions.Should().ContainSingle(row =>
            row.Code == ApplicationErrorCodes.ResolutionReleaseBlockedByMatchesRunning);
        running.Plan.Releases.Should().BeEmpty();

        referenced.Plan.Exclusions.Should().ContainSingle(row =>
            row.Code == ApplicationErrorCodes.ResolutionReleaseNotReleasable
            && row.Target == StructureResolutionOccupantTarget.Population);
        referenced.Plan.Releases.Should().BeEmpty();
    }

    [Fact]
    public void A_result_change_conflicts_and_leaves_the_place()
    {
        var (competition, league, terminal, entries, match) = OpenSelection();
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);
        match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 3)), _clock);

        var act = () => ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, Places(planned), stages, matches, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseConflict);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(entries[0]);
    }

    [Fact]
    public void Withdrawing_every_active_entry_is_no_longer_definitive()
    {
        var (competition, league, terminal, entries, match) = OpenSelection();
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);
        var token = planned.VersionToken;
        competition.WithdrawEntry(entries[0], _clock);
        competition.WithdrawEntry(entries[1], _clock);

        MyClub.PlayUp.Application.Stages.CascadeReplace.CascadeReplaceVersion
            .Compute(stages, matches).Should().Be(token);
        var act = () => ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, token, Places(planned), stages, matches, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseNotDefinitive);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(entries[0]);
    }

    [Fact]
    public void A_different_place_list_conflicts_without_writing()
    {
        var (competition, league, terminal, entries, match) = League();
        Stamp(league, terminal, ("A", entries[0]), ("B", entries[1]));
        Complete(league);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);
        var firstOnly = Places(planned).Take(1).ToArray();

        var act = () => ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, firstOnly, stages, matches, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseBatchChanged);
        terminal.FindSlot("A")!.EntryId.Should().Be(entries[0]);
        terminal.FindSlot("B")!.EntryId.Should().Be(entries[1]);
    }

    [Fact]
    public void Completing_the_competition_changes_the_release_list()
    {
        var (competition, league, terminal, entries, match) = League();
        terminal.AddSlot("Egal");
        terminal.AddSlot("Vide");
        var blocked = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForSlot(terminal.Id, "Egal"),
            QualificationCondition.PointsAtLeast(100));
        var empty = Path(2, QualificationDestination.ForSlot(terminal.Id, "Vide"));
        league.ReplaceQualificationRules(new QualificationRules([blocked, empty]), _clock);
        terminal.AddResolvedPopulationEntry(entries[0], _clock, Provenance(league, blocked));
        terminal.ApplyResolvedEntry("Egal", entries[0], _clock, Provenance(league, blocked));
        terminal.AddResolvedPopulationEntry(entries[1], _clock, Provenance(league, empty));
        terminal.ApplyResolvedEntry("Vide", entries[1], _clock, Provenance(league, empty));
        Complete(league);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);
        Primary(planned).Should().ContainSingle(row => row.SlotKey == "Vide");

        competition.Complete(CompletionMode.Normal, _clock);
        var act = () => ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, Places(planned), stages, matches, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseBatchChanged);
        terminal.FindSlot("Vide")!.EntryId.Should().Be(entries[1]);
        terminal.FindSlot("Egal")!.EntryId.Should().Be(entries[0]);
    }

    [Fact]
    public void The_structure_read_the_preview_and_the_confirmation_judge_the_same_places()
    {
        var (competition, league, terminal, entries, match) = League();
        Stamp(league, terminal, ("A", entries[0]), ("B", entries[1]));
        var third = AddEntry.Execute(competition, "Est", _clock).Id;
        var cancelled = Match.Create(competition.Id, terminal.Id, entries[1], third, _clock);
        cancelled.Cancel(_clock);
        Complete(league);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match, cancelled };
        var summaries = new Dictionary<StageId, IReadOnlyList<MyClub.PlayUp.Application.Abstractions.MatchSummaryRow>>
        {
            [league.Id] = [],
            [terminal.Id] = [],
        };
        summaries[league.Id] =
        [
            new(
                match.Id,
                match.StageId,
                match.Status,
                match.HomeEntryId,
                match.AwayEntryId,
                match.Result,
                match.HasObservedLive),
        ];
        summaries[terminal.Id] =
        [
            new(
                cancelled.Id,
                cancelled.StageId,
                cancelled.Status,
                cancelled.HomeEntryId,
                cancelled.AwayEntryId,
                cancelled.Result,
                cancelled.HasObservedLive),
        ];

        var occupants = ResolutionFreshnessDetector.Evaluate(competition, stages, summaries)
            .SelectMany(pair => pair.Value)
            .Where(occupant => occupant.Target == StructureResolutionOccupantTarget.Slot)
            .ToArray();
        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);

        occupants.Should().Contain(occupant => occupant.SlotKey == "A" && occupant.Definitive && occupant.Releasable);
        occupants.Should().Contain(occupant => occupant.SlotKey == "B" && occupant.Definitive && !occupant.Releasable);
        Primary(planned).Select(row => row.SlotKey).Should().Equal("A");
        planned.Exclusions.Where(row => row.Target == StructureResolutionOccupantTarget.Slot)
            .Select(row => row.SlotKey).Should().Equal("B");

        ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, Places(planned), stages, matches, _clock);
        terminal.FindSlot("A")!.EntryId.Should().BeNull();
        terminal.FindSlot("B")!.EntryId.Should().Be(entries[1]);
    }

    [Fact]
    public void A_completed_source_refuses_a_qualification_rule_edit()
    {
        var (_, league, terminal, entries, _) = League();
        Stamp(league, terminal, ("Champ", entries[0]));
        Complete(league);

        var act = () => league.ReplaceQualificationRules(
            new QualificationRules([Path(2, QualificationDestination.ForSlot(terminal.Id, "Champ"))]),
            _clock);

        // A rule change cannot reach the grouped confirmation: the source is frozen once completed.
        // Completing the competition is the stand-in that still changes the token.
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    [Fact]
    public void A_deleted_source_is_released_with_the_destination()
    {
        var (competition, league, terminal, entries, match) = League();
        Stamp(league, terminal, ("Champ", entries[0]));
        competition.RemoveStage(league.Id, _clock);
        var stages = new List<Stage> { terminal };
        var matches = new List<Match> { match };

        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, null, terminal.Id, stages, matches, _clock);

        Primary(planned).Select(row => row.SlotKey).Should().Equal("Champ");
        planned.Releases.Should().Contain(row => row.SideEffect && row.Target == StructureResolutionOccupantTarget.Population);
        planned.Exclusions.Should().NotContain(row => row.Code == ApplicationErrorCodes.ResolutionReleaseOccupantNotFound);
        ReleaseDefinitiveResolutions.Execute(
            competition, null, terminal.Id, planned.VersionToken, Places(planned), stages, matches, _clock);
        terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void A_living_source_is_outside_the_deleted_source_scope()
    {
        var (competition, league, terminal, entries, match) = League();
        Stamp(league, terminal, ("Champ", entries[0]));
        Complete(league);

        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, null, terminal.Id, [league, terminal], [match], _clock);

        planned.Releases.Should().BeEmpty();
        planned.Exclusions.Should().BeEmpty();
    }

    [Fact]
    public void A_released_definitive_place_can_be_assigned_by_hand()
    {
        var (competition, league, terminal, entries, match) = League();
        Stamp(league, terminal, ("Champ", entries[0]));
        Complete(league);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);
        ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, Places(planned), stages, matches, _clock);
        var libre = AddEntry.Execute(competition, "Libre", _clock).Id;
        terminal.AddResolvedPopulationEntry(libre, _clock);

        terminal.AssignEntryToSlot("Champ", libre);

        terminal.FindSlot("Champ")!.EntryId.Should().Be(libre);
    }

    [Fact]
    public void Manual_overwrite_stays_closed_when_the_source_is_not_definitive()
    {
        var (competition, league, _, _, _) = League();
        ManualPlacementSource.AllowsOverwrite(competition, league, league.Id.Value).Should().BeFalse();
        Complete(league);
        ManualPlacementSource.AllowsOverwrite(competition, league, league.Id.Value).Should().BeTrue();
        ManualPlacementSource.AllowsOverwrite(competition, null, league.Id.Value).Should().BeFalse();
        competition.RemoveStage(league.Id, _clock);
        ManualPlacementSource.AllowsOverwrite(competition, null, league.Id.Value).Should().BeTrue();
        ManualPlacementSource.AllowsOverwrite(competition, null, null).Should().BeFalse();
    }

    [Fact]
    public void Sixteen_places_are_released_without_a_quadratic_blow_up()
    {
        var competition = CreateCompetition.Execute("Seize", _clock);
        var entries = Enumerable.Range(0, 16)
            .Select(index => AddEntry.Execute(competition, $"E{index:00}", _clock).Id)
            .ToArray();
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        competition.AddStage(terminal.Id, _clock);
        var paths = new QualificationPath[16];
        for (var index = 0; index < 16; index++)
        {
            var key = $"S{index:00}";
            terminal.AddSlot(key);
            paths[index] = Path(index + 1, QualificationDestination.ForSlot(terminal.Id, key));
        }

        league.ReplaceQualificationRules(new QualificationRules(paths), _clock);
        for (var index = 0; index < 16; index++)
        {
            var provenance = Provenance(league, paths[index]);
            terminal.AddResolvedPopulationEntry(entries[index], _clock, provenance);
            terminal.ApplyResolvedEntry($"S{index:00}", entries[index], _clock, provenance);
        }

        Complete(league);
        var match = Match.Create(competition.Id, league.Id, entries[0], entries[1], _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        var watch = Stopwatch.StartNew();

        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, stages, matches, _clock);
        ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, Places(planned), stages, matches, _clock);
        watch.Stop();

        Primary(planned).Should().HaveCount(16);
        terminal.Slots.Should().OnlyContain(slot => slot.EntryId == null);

        // Each place copies the two phases and re-inspects every occupant, so the cost
        // grows with places × occupants. Ten seconds is a ceiling for this 16-place batch.
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Four_places_stay_under_the_same_ceiling()
    {
        var competition = CreateCompetition.Execute("Quatre", _clock);
        var entries = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"E{index:00}", _clock).Id)
            .ToArray();
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        competition.AddStage(terminal.Id, _clock);
        var seats = entries.Select((entry, index) => ($"S{index}", terminal, entry)).ToArray();
        Stamp(league, seats);
        Complete(league);
        var match = Match.Create(competition.Id, league.Id, entries[0], entries[1], _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var watch = Stopwatch.StartNew();

        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, [league, terminal], [match], _clock);
        ReleaseDefinitiveResolutions.Execute(
            competition, league.Id, null, planned.VersionToken, Places(planned), [league, terminal], [match], _clock);
        watch.Stop();

        Primary(planned).Should().HaveCount(4);
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void A_destination_filter_keeps_one_phase()
    {
        var (competition, league, first, entries, match) = League();
        var second = Stage.Create(competition.Id, new StageName("Autre"), SampleRegulations.Standard(), _clock);
        competition.AddStage(second.Id, _clock);
        Stamp(league, ("Pres", first, entries[0]), ("Loin", second, entries[1]));
        Complete(league);
        var stages = new List<Stage> { league, first, second };
        var matches = new List<Match> { match };

        var planned = ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, first.Id, stages, matches, _clock);

        Primary(planned).Should().ContainSingle(row => row.SlotKey == "Pres" && row.StageName == "Terminal");
        planned.Exclusions.Should().BeEmpty();
    }

    private static QualificationPath Path(
        int order,
        QualificationDestination destination,
        SelectionMode mode = SelectionMode.Position,
        int value = 99) =>
        new(order, QualificationSource.Overall(), new QualificationSelection(mode, value), destination);

    private static ResolutionProvenance Provenance(Stage league, QualificationPath path) =>
        new(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");

    private static DefinitiveResolutionReleaseRow[] Primary(DefinitiveResolutionReleaseResult result) =>
        result.Releases.Where(row => !row.SideEffect).ToArray();

    private static DefinitiveResolutionReleasePlace[] Places(DefinitiveResolutionReleaseResult result) =>
        Primary(result)
            .Select(row => new DefinitiveResolutionReleasePlace(
                row.StageId, row.Target, row.EntryId, row.SlotKey, row.GroupId))
            .ToArray();

    private (DefinitiveResolutionReleaseResult Plan, Stage Terminal) DrawWorld()
    {
        var (competition, league, terminal, entries, match) = OpenSelection();
        var draw = terminal.CreateDraw(DrawResolutionKind.Slot, _clock);
        terminal.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entries[0]]));
        terminal.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entries[0], "Champ")]),
            _clock);
        terminal.PublishDraw(draw.Id, _clock);
        return (ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, [league, terminal], [match], _clock), terminal);
    }

    private (DefinitiveResolutionReleaseResult Plan, Stage Terminal) RunningWorld()
    {
        var (competition, league, terminal, entries, match) = League();
        Stamp(league, terminal, ("Champ", entries[0]));
        var matchday = terminal.AddMatchday(1, _clock);
        var fixture = terminal.AddFixture(matchday.Id, _clock);
        var attached = Match.Create(competition.Id, terminal.Id, entries[0], entries[1], _clock);
        terminal.AttachMatch(fixture.Id, attached.Id, 1, _clock);
        terminal.Prepare(_clock);
        terminal.Start(_clock);
        Complete(league);
        return (ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, [league, terminal], [match, attached], _clock), terminal);
    }

    private (DefinitiveResolutionReleaseResult Plan, Stage Terminal) ReferencedWorld()
    {
        var (competition, league, terminal, entries, match) = League();
        var path = Path(1, QualificationDestination.ForPopulation(terminal.Id));
        league.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = Provenance(league, path);
        terminal.AddSlot("Fixe");
        terminal.AddResolvedPopulationEntry(entries[0], _clock, provenance);
        terminal.AssignEntryToSlot("Fixe", entries[0]);
        Complete(league);
        return (ReleaseDefinitiveResolutions.Plan(
            competition, league.Id, null, [league, terminal], [match], _clock), terminal);
    }

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, Match Match) OpenSelection()
    {
        var world = League();
        Stamp(world.League, world.Terminal, ("Champ", world.Entries[0]));
        Complete(world.League);
        world.Competition.Prepare(_clock);
        world.Competition.Start(_clock);
        return world;
    }

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, Match Match) League()
    {
        var competition = CreateCompetition.Execute($"Lot {Guid.NewGuid():N}", _clock);
        var entries = new[]
        {
            AddEntry.Execute(competition, "Nord", _clock).Id,
            AddEntry.Execute(competition, "Sud", _clock).Id,
        };
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        competition.AddStage(terminal.Id, _clock);
        var match = Match.Create(competition.Id, league.Id, entries[0], entries[1], _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        return (competition, league, terminal, entries, match);
    }

    private void Stamp(Stage league, Stage terminal, params (string Slot, EntryId Entry)[] seats) =>
        Stamp(league, seats.Select(seat => (seat.Slot, terminal, seat.Entry)).ToArray());

    private void Stamp(Stage league, params (string Slot, Stage Terminal, EntryId Entry)[] seats)
    {
        var paths = seats.Select((seat, index) =>
        {
            seat.Terminal.AddSlot(seat.Slot);
            return Path(index + 1, QualificationDestination.ForSlot(seat.Terminal.Id, seat.Slot));
        }).ToArray();
        league.ReplaceQualificationRules(new QualificationRules(paths), _clock);
        for (var index = 0; index < seats.Length; index++)
        {
            var provenance = Provenance(league, paths[index]);
            seats[index].Terminal.AddResolvedPopulationEntry(seats[index].Entry, _clock, provenance);
            seats[index].Terminal.ApplyResolvedEntry(seats[index].Slot, seats[index].Entry, _clock, provenance);
        }
    }

    private void Complete(Stage stage)
    {
        if (stage.Matchdays.Count == 0 && stage.Rounds.Count == 0 && stage.Groups.Count == 0)
        {
            stage.AddMatchday(1, _clock);
        }

        if (stage.Status == StageStatus.Draft)
        {
            stage.Prepare(_clock);
        }

        if (stage.Status == StageStatus.Ready)
        {
            stage.Start(_clock);
        }

        stage.Complete(_clock);
    }
}
