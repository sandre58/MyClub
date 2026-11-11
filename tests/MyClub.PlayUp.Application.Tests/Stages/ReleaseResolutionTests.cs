// -----------------------------------------------------------------------
// <copyright file="ReleaseResolutionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Stages.ReleaseResolution;
using MyClub.PlayUp.Application.Standings;
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

public sealed class ReleaseResolutionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 16, 0, 0, TimeSpan.Zero));

    private static ResolutionReleaseTarget SlotTarget(EntryId entry) =>
        new(StructureResolutionOccupantTarget.Slot, entry.Value, "Champ", null);

    private static Dictionary<StageId, IReadOnlyList<MyClub.PlayUp.Application.Abstractions.MatchSummaryRow>> Summaries(
        Stage stage,
        Match match) =>
        new Dictionary<StageId, IReadOnlyList<MyClub.PlayUp.Application.Abstractions.MatchSummaryRow>>
        {
            [stage.Id] =
            [
                new(
                    match.Id,
                    match.StageId,
                    match.Status,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    match.Result,
                    match.HasObservedLive)
            ]
        };

    [Fact]
    public void Preview_writes_nothing_and_confirmation_clears_an_empty_selection()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        var target = SlotTarget(entries[0]);

        var planned = ReleaseResolution.Plan(competition, terminal.Id, target, stages, matches, _clock);

        terminal.FindSlot("Champ")!.EntryId.Should().Be(entries[0]);
        planned.Releases.Should().ContainSingle().Which.EntryId.Should().Be(entries[0].Value);
        planned.DetachedMatches.Should().BeEmpty();

        var result = ReleaseResolution.Execute(competition, terminal.Id, target, stages, matches, _clock);

        result.VersionToken.Should().Be(planned.VersionToken);
        terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
        terminal.FindSlot("Champ")!.ResolutionProvenance.Should().BeNull();
        var again = () => ReleaseResolution.Plan(competition, terminal.Id, target, stages, matches, _clock);
        again.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseOccupantNotFound);
    }

    [Theory]
    [InlineData(StageStatus.Draft)]
    [InlineData(StageStatus.Ready)]
    [InlineData(StageStatus.Running)]
    [InlineData(StageStatus.Suspended)]
    public void An_unconfirmed_slot_can_be_released_from_an_open_phase(StageStatus status)
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        Move(terminal, status);

        ReleaseResolution.Execute(
            competition,
            terminal.Id,
            SlotTarget(entries[0]),
            [league, terminal],
            [match],
            _clock);

        terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
        terminal.Status.Should().Be(status);
    }

    [Fact]
    public void A_completed_phase_refuses_the_release()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        Move(terminal, StageStatus.Completed);

        var act = () => ReleaseResolution.Plan(
            competition,
            terminal.Id,
            SlotTarget(entries[0]),
            [league, terminal],
            [match],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseCompleted);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(entries[0]);
    }

    [Fact]
    public void A_completed_downstream_phase_does_not_block_the_released_place()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        var downstream = Stage.Create(competition.Id, new StageName("Aval"), SampleRegulations.Standard(), _clock);
        downstream.AddMatchday(1, _clock);
        competition.AddStage(downstream.Id, _clock);
        Move(downstream, StageStatus.Completed);

        ReleaseResolution.Execute(
            competition,
            terminal.Id,
            SlotTarget(entries[0]),
            [league, terminal, downstream],
            [match],
            _clock);

        terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void A_current_place_is_not_indeterminate_and_a_direct_assignment_is_not_released()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 1);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };

        var current = () => ReleaseResolution.Plan(
            competition, terminal.Id, SlotTarget(entries[0]), stages, matches, _clock);
        current.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseNotIndeterminate);

        var direct = Stage.Create(competition.Id, new StageName("Direct"), SampleRegulations.Standard(), _clock);
        direct.AddSlot("Fixe");
        competition.AddStage(direct.Id, _clock);
        direct.AddResolvedPopulationEntry(entries[1], _clock);
        direct.AssignEntryToSlot("Fixe", entries[1]);
        var held = () => ReleaseResolution.Plan(
            competition,
            direct.Id,
            new ResolutionReleaseTarget(StructureResolutionOccupantTarget.Slot, entries[1].Value, "Fixe", null),
            [league, terminal, direct],
            matches,
            _clock);
        held.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseDirectAssignment);
    }

    [Fact]
    public void Confirmation_rechecks_the_state_when_the_token_is_unchanged()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 1, minimumPoints: 100);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        var target = SlotTarget(entries[0]);
        var preview = ReleaseResolution.Plan(competition, terminal.Id, target, stages, matches, _clock);

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(terminal.Id, "Champ"))
            ]),
            _clock);

        var token = MyClub.PlayUp.Application.Stages.CascadeReplace.CascadeReplaceVersion.Compute(stages, matches);
        token.Should().Be(preview.VersionToken);
        var act = () => ReleaseResolution.Execute(competition, terminal.Id, target, stages, matches, _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseNotIndeterminate);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(entries[0]);
    }

    [Fact]
    public void A_configuration_exception_cannot_be_released()
    {
        var competition = CreateCompetition.Execute("Config", _clock);
        var entry = AddEntry.Execute(competition, "Nord", _clock).Id;
        var source = Stage.Create(competition.Id, new StageName("Tableau"), SampleRegulations.Standard(), _clock);
        source.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        competition.AddStage(source.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Suite"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForSlot(terminal.Id, "Champ"));
        source.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(source.Id, path),
            "seed");
        terminal.AddResolvedPopulationEntry(entry, _clock, provenance);
        terminal.ApplyResolvedEntry("Champ", entry, _clock, provenance);

        var occupants = ResolutionFreshnessDetector.Evaluate(competition, [source, terminal], null)
            .SelectMany(pair => pair.Value);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Champ"
            && occupant.IndeterminacyCause == StructureResolutionIndeterminacyCause.ConfigurationInvalide
            && !occupant.Releasable);
        var act = () => ReleaseResolution.Plan(
            competition,
            terminal.Id,
            SlotTarget(entry),
            [source, terminal],
            [],
            _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseNotReleasable);
    }

    [Fact]
    public void A_population_still_referenced_by_a_group_is_refused()
    {
        var (competition, league, terminal, entries, match) = Pair();
        var path = PopulationPath(league, terminal, position: 9);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");
        var group = terminal.AddGroup("A", _clock);
        terminal.AddResolvedPopulationEntry(entries[0], _clock, provenance);
        terminal.ApplyResolvedGroupEntry(group.Id, entries[0], provenance);
        var occupants = ResolutionFreshnessDetector.Evaluate(competition, [league, terminal], Summaries(league, match))
            .SelectMany(pair => pair.Value);
        occupants.Should().Contain(occupant =>
            occupant.Target == StructureResolutionOccupantTarget.Population && !occupant.Releasable);

        var act = () => ReleaseResolution.Plan(
            competition,
            terminal.Id,
            new ResolutionReleaseTarget(StructureResolutionOccupantTarget.Population, entries[0].Value, null, null),
            [league, terminal],
            [match],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseNotReleasable);
        terminal.CompositionEntries.Should().Contain(entry => entry.EntryId.Equals(entries[0]));
    }

    [Fact]
    public void Releasing_a_population_removes_the_witness_and_a_later_apply_writes_it_again()
    {
        var (competition, league, terminal, entries, match) = Pair();
        var path = PopulationPath(league, terminal, position: 9);
        var winner = entries[0];
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");
        terminal.AddResolvedPopulationEntry(winner, _clock, provenance);
        terminal.RecordFormPathResolution(
            FormPathResolutionKey.FromQualification(league.Id, path),
            winner);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };
        ResolutionFreshnessDetector.Evaluate(competition, stages, Summaries(league, match))
            .SelectMany(pair => pair.Value)
            .Should().Contain(occupant =>
                occupant.Target == StructureResolutionOccupantTarget.Population
                && occupant.State == StructureResolutionFreshness.Indetermine
                && occupant.Releasable);

        ReleaseResolution.Execute(
            competition,
            terminal.Id,
            new ResolutionReleaseTarget(StructureResolutionOccupantTarget.Population, winner.Value, null, null),
            stages,
            matches,
            _clock);

        terminal.FormPathResolutions.Should().NotContain(resolution => resolution.EntryId.Equals(winner));
        terminal.CompositionEntries.Should().NotContain(entry => entry.EntryId.Equals(winner));
        DestinationSatisfaction.EvaluateQualification(terminal, league.Id, path, winner)
            .Should().Be(DestinationSatisfaction.Kind.Pending);
        InboundFeedSeatResolver.ResolveQualificationEntry(path, terminal, league, Summaries(league, match))
            .Should().BeNull();
        CupInboundSlotEntryResolver.TryResolve(terminal, "Champ", stages, Summaries(league, match))
            .Should().BeNull();

        var resolved = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForPopulation(terminal.Id));
        league.ReplaceQualificationRules(new QualificationRules([resolved]), _clock);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules!);
        ApplyQualification.Execute(league, standing, stages, _clock);

        terminal.FormPathResolutions.Should().Contain(resolution => resolution.EntryId.Equals(winner));
        DestinationSatisfaction.EvaluateQualification(terminal, league.Id, resolved, winner)
            .Should().Be(DestinationSatisfaction.Kind.Satisfied);
    }

    [Fact]
    public void Releasing_a_progression_slot_lets_a_later_apply_fill_it_again()
    {
        var (competition, source, destination, match, fixture, winner) = Progression();
        ApplyProgressionOutcome.Execute(source, fixture.Id, [match], [source, destination], _clock);
        destination.FindSlot("Jouer")!.EntryId.Should().Be(winner);
        match.InvalidateFinishedResult(_clock);

        ReleaseResolution.Execute(
            competition,
            destination.Id,
            new ResolutionReleaseTarget(StructureResolutionOccupantTarget.Slot, winner.Value, "Jouer", null),
            [source, destination],
            [match],
            _clock);
        destination.FindSlot("Jouer")!.EntryId.Should().BeNull();

        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        ApplyProgressionOutcome.Execute(source, fixture.Id, [match], [source, destination], _clock);

        destination.FindSlot("Jouer")!.EntryId.Should().Be(winner);
    }

    [Fact]
    public void A_two_leg_bracket_clears_the_team_side_on_each_leg()
    {
        var (competition, league, terminal, entries, sourceMatch) = Pair();
        var opponent = entries[1];
        var team = entries[0];
        terminal.AddSlot("B");
        terminal.AddRound("Finale", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        terminal.ReplaceBracketPairs([new BracketPair("P1", "Champ", "B")]);
        var fixture = terminal.AddFixture(terminal.Rounds[0].Id, _clock, "Champ", "B", "P1");
        StampSlot(league, terminal, opponent, position: 1, slot: "B");
        StampSlot(league, terminal, team, position: 9);
        var leg1 = Match.Create(competition.Id, terminal.Id, team, opponent, _clock);
        var leg2 = Match.Create(competition.Id, terminal.Id, opponent, team, _clock);
        terminal.AttachMatch(fixture.Id, leg1.Id, 1, _clock);
        terminal.AttachMatch(fixture.Id, leg2.Id, 2, _clock);

        var result = ReleaseResolution.Execute(
            competition,
            terminal.Id,
            SlotTarget(team),
            [league, terminal],
            [sourceMatch, leg1, leg2],
            _clock);

        leg1.HomeEntryId.Should().BeNull();
        leg1.AwayEntryId.Should().Be(opponent);
        leg2.HomeEntryId.Should().Be(opponent);
        leg2.AwayEntryId.Should().BeNull();
        result.DetachedMatches.Should().HaveCount(2);
        result.DetachedMatches.Should().Contain(row => row.LegIndex == 1 && row.Side == Side.Home);
        result.DetachedMatches.Should().Contain(row => row.LegIndex == 2 && row.Side == Side.Away);
        terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
        terminal.FindSlot("B")!.EntryId.Should().Be(opponent);
    }

    [Fact]
    public void A_non_bracket_place_with_an_attached_match_is_refused()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        var matchday = terminal.AddMatchday(1, _clock);
        var fixture = terminal.AddFixture(matchday.Id, _clock);
        var attached = Match.Create(competition.Id, terminal.Id, entries[0], entries[1], _clock);
        terminal.AttachMatch(fixture.Id, attached.Id, 1, _clock);

        var draft = () => ReleaseResolution.Plan(
            competition,
            terminal.Id,
            SlotTarget(entries[0]),
            [league, terminal],
            [match, attached],
            _clock);
        draft.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseBlockedByMatches);

        Move(terminal, StageStatus.Running);
        var running = () => ReleaseResolution.Plan(
            competition,
            terminal.Id,
            SlotTarget(entries[0]),
            [league, terminal],
            [match, attached],
            _clock);
        running.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseBlockedByMatchesRunning);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(entries[0]);
    }

    [Fact]
    public void A_live_or_finished_match_on_the_phase_blocks_the_release()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        var played = Match.Create(competition.Id, terminal.Id, entries[0], entries[1], _clock);
        played.Start(_clock);
        played.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var act = () => ReleaseResolution.Plan(
            competition,
            terminal.Id,
            SlotTarget(entries[0]),
            [league, terminal],
            [match, played],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseBlockedByPlayedMatch);
    }

    [Fact]
    public void A_team_held_by_a_published_or_cancelled_draw_cannot_be_released()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        var draw = terminal.CreateDraw(DrawResolutionKind.Slot, _clock);
        terminal.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entries[0]]));
        terminal.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entries[0], "Champ")]),
            _clock);
        terminal.PublishDraw(draw.Id, _clock);
        var stages = new List<Stage> { league, terminal };
        var matches = new List<Match> { match };

        var published = () => ReleaseResolution.Plan(
            competition, terminal.Id, SlotTarget(entries[0]), stages, matches, _clock);
        published.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseBlockedByDraw);

        terminal.CancelDraw(draw.Id, _clock);
        var cancelled = () => ReleaseResolution.Plan(
            competition, terminal.Id, SlotTarget(entries[0]), stages, matches, _clock);
        cancelled.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ResolutionReleaseBlockedByDraw);
    }

    [Fact]
    public void A_draft_draw_does_not_block_the_release()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        var draw = terminal.CreateDraw(DrawResolutionKind.Slot, _clock);
        terminal.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entries[0]]));

        ReleaseResolution.Execute(
            competition,
            terminal.Id,
            SlotTarget(entries[0]),
            [league, terminal],
            [match],
            _clock);

        terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
        draw.Status.Should().Be(DrawStatus.Draft);
    }

    [Fact]
    public void Affected_occupants_match_a_full_recalculation()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);
        var further = Stage.Create(competition.Id, new StageName("Suite"), SampleRegulations.Standard(), _clock);
        further.AddSlot("Suite");
        competition.AddStage(further.Id, _clock);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForSlot(further.Id, "Suite"));
        terminal.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(terminal.Id, path),
            "seed");
        further.AddResolvedPopulationEntry(entries[0], _clock, provenance);
        further.ApplyResolvedEntry("Suite", entries[0], _clock, provenance);
        var stages = new List<Stage> { league, terminal, further };
        var matches = new List<Match> { match };
        var before = ResolutionFreshnessDetector.Inspect(competition, stages, Summaries(league, match));

        var result = ReleaseResolution.Execute(
            competition,
            terminal.Id,
            SlotTarget(entries[0]),
            stages,
            matches,
            _clock);
        var after = ResolutionFreshnessDetector.Inspect(competition, stages, Summaries(league, match));

        var expected = before
            .Where(occupant => occupant.StageId != terminal.Id.Value || occupant.SlotKey != "Champ")
            .Select(occupant => new
            {
                occupant,
                next = after.FirstOrDefault(candidate =>
                    candidate.StageId == occupant.StageId
                    && candidate.Target == occupant.Target
                    && candidate.EntryId == occupant.EntryId
                    && candidate.SlotKey == occupant.SlotKey
                    && candidate.GroupId == occupant.GroupId)
            })
            .Where(pair => pair.next is not null && pair.next.State != pair.occupant.State)
            .Select(pair => new ResolutionReleaseAffectedOccupant(
                pair.occupant.StageId,
                pair.occupant.Target,
                pair.occupant.SlotKey,
                pair.occupant.GroupId,
                pair.occupant.EntryId,
                pair.occupant.State,
                pair.next!.State))
            .ToArray();
        result.AffectedOccupants.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Releasing_a_group_in_draft_leaves_the_seat_reserved_for_the_feed()
    {
        var (competition, league, terminal, entries, match) = Pair();
        var group = terminal.AddGroup("A", _clock);
        terminal.SetPlacesPerGroup(2);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 9),
            QualificationDestination.ForGroup(terminal.Id, group.Id));
        league.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");
        terminal.AddResolvedPopulationEntry(entries[0], _clock, provenance);
        terminal.AddResolvedPopulationEntry(entries[1], _clock);
        terminal.ApplyResolvedGroupEntry(group.Id, entries[0], provenance);
        terminal.ApplyResolvedGroupEntry(
            group.Id,
            entries[1],
            new ResolutionProvenance(ResolutionProvenanceKind.Directe, "direct", "seed"));

        ReleaseResolution.Execute(
            competition,
            terminal.Id,
            new ResolutionReleaseTarget(StructureResolutionOccupantTarget.Group, entries[0].Value, null, group.Id.Value),
            [league, terminal],
            [match],
            _clock);

        group.EntryIds.Should().Equal(entries[1]);
        var pending = GroupPendingFeeds.CountUnresolved(terminal, [league, terminal], group.Id);
        var seat = ManualGroupSeatRules.Project(
            terminal,
            [league, terminal],
            group,
            index: 2,
            occupant: null,
            terminal.CompositionEntries.Select(entry => entry.EntryId).ToArray(),
            [],
            pending);
        seat.SeatMode.Should().Be(GroupSeatModes.Reserved);
    }

    [Fact]
    public void An_unconfirmed_qualification_is_releasable_and_a_current_one_is_not()
    {
        var (competition, league, terminal, entries, match) = Pair();
        StampSlot(league, terminal, entries[0], position: 9);

        var occupants = ResolutionFreshnessDetector.Evaluate(
            competition,
            [league, terminal],
            Summaries(league, match)).SelectMany(pair => pair.Value);

        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Champ" && occupant.Releasable);
        StampSlot(league, terminal, entries[0], position: 1);
        ResolutionFreshnessDetector.Evaluate(competition, [league, terminal], Summaries(league, match))
            .SelectMany(pair => pair.Value)
            .Should().Contain(occupant => occupant.SlotKey == "Champ" && !occupant.Releasable);
    }

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, Match Match) Pair()
    {
        var competition = CreateCompetition.Execute($"Release {Guid.NewGuid():N}", _clock);
        var entries = new[]
        {
            AddEntry.Execute(competition, "Nord", _clock).Id,
            AddEntry.Execute(competition, "Sud", _clock).Id,
        };
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);
        var match = Match.Create(competition.Id, league.Id, entries[0], entries[1], _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        return (competition, league, terminal, entries, match);
    }

    private void StampSlot(
        Stage league,
        Stage terminal,
        EntryId occupant,
        int position,
        string slot = "Champ",
        int? minimumPoints = null)
    {
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForSlot(terminal.Id, slot),
            minimumPoints is int points ? QualificationCondition.PointsAtLeast(points) : null);
        league.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");
        if (terminal.CompositionEntries.All(entry => !entry.EntryId.Equals(occupant)))
        {
            terminal.AddResolvedPopulationEntry(occupant, _clock, provenance);
        }

        terminal.ApplyResolvedEntry(slot, occupant, _clock, provenance);
    }

    private QualificationPath PopulationPath(Stage league, Stage terminal, int position)
    {
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForPopulation(terminal.Id));
        league.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        return path;
    }

    private (Competition Competition, Stage Source, Stage Destination, Match Match, Fixture Fixture, EntryId Winner) Progression()
    {
        var competition = CreateCompetition.Execute("Progression release", _clock);
        var winner = AddEntry.Execute(competition, "Nord", _clock).Id;
        var other = AddEntry.Execute(competition, "Sud", _clock).Id;
        var source = Stage.Create(competition.Id, new StageName("Barrage"), SampleRegulations.Standard(), _clock);
        source.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("A");
        source.AddSlot("B");
        source.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        competition.AddStage(source.Id, _clock);
        source.AddResolvedPopulationEntry(winner, _clock);
        source.AddResolvedPopulationEntry(other, _clock);
        source.AssignEntryToSlot("A", winner);
        source.AssignEntryToSlot("B", other);
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock, "A", "B", "P1");
        var match = Match.Create(competition.Id, source.Id, winner, other, _clock);
        source.AttachMatch(fixture.Id, match.Id, 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        var destination = Stage.Create(competition.Id, new StageName("Suite"), SampleRegulations.Standard(), _clock);
        destination.AddSlot("Jouer");
        competition.AddStage(destination.Id, _clock);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(destination.Id, "Jouer"))
            ]),
            _clock);
        return (competition, source, destination, match, fixture, winner);
    }

    private void Move(Stage stage, StageStatus status)
    {
        if (status == StageStatus.Draft)
        {
            return;
        }

        if (stage.Matchdays.Count == 0 && stage.Rounds.Count == 0 && stage.Groups.Count == 0)
        {
            stage.AddMatchday(1, _clock);
        }

        if (stage.Status == StageStatus.Draft)
        {
            stage.Prepare(_clock);
        }

        if (status == StageStatus.Ready)
        {
            return;
        }

        if (stage.Status == StageStatus.Ready)
        {
            stage.Start(_clock);
        }

        if (status == StageStatus.Running)
        {
            return;
        }

        if (status == StageStatus.Suspended)
        {
            stage.Suspend(_clock);
            return;
        }

        stage.Complete(_clock);
    }
}
