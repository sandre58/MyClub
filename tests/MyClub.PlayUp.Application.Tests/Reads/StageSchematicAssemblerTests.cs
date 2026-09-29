// -----------------------------------------------------------------------
// <copyright file="StageSchematicAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StageSchematicAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Cup_empty_slots_have_no_connections_without_fixtures()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.FormatKind.Should().Be(StructureFormatKind.Cup);
        schematic.Cases.Should().HaveCount(2);
        schematic.Cases.Should().OnlyContain(c => c.Entry == null && c.Assignment == null);
        schematic.Cases.Should().OnlyContain(c => c.FormPosition.Kind == StageSchematicAssembler.FormKindCupSlot);

        // No BracketPairs → no invented Side / PairOrdinal from slot adjacency.
        schematic.Cases.Should().OnlyContain(c => c.FormPosition.Side == null && c.FormPosition.PairOrdinal == null);
        schematic.Connections.Should().BeEmpty();
    }

    [Fact]
    public void Cup_bracket_pairs_without_fixtures_expose_structural_connections()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.AddSlot("S3");
        stage.AddSlot("S4");
        stage.SeedEntryRoundBracketPairs();

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.Connections.Should().HaveCount(2);
        schematic.Connections.Should().OnlyContain(c => c.FixtureId == null && c.MatchNumber == 0);
        schematic.Connections.Select(c => c.PairKey).Should().BeEquivalentTo("P1", "P2");
        schematic.Cases.Single(c => c.FormPosition.SlotKey == "S1").FormPosition.Side.Should().Be("A");
        schematic.Cases.Single(c => c.FormPosition.SlotKey == "S2").FormPosition.Side.Should().Be("B");
        schematic.Cases.Single(c => c.FormPosition.SlotKey == "S1").FormPosition.PairOrdinal.Should().Be(1);
    }

    [Fact]
    public void Cup_placed_slot_and_fixture_expose_connection_match_number()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var alpha = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var stage = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "SF1-A", "SF1-B")]);
        stage.ReplaceAffectationAuthoring([alpha.Id], _clock);
        stage.AssignEntryToSlot("SF1-A", alpha.Id);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A", "SF1-B", "P1");

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        var placed = schematic.Cases.Single(c => c.FormPosition.SlotKey == "SF1-A");
        placed.Entry!.EntryId.Should().Be(alpha.Id.Value);
        placed.Entry.DisplayName.Should().Be("Alpha");
        placed.Assignment!.DisplayName.Should().Be("Alpha");
        schematic.Cases.Single(c => c.FormPosition.SlotKey == "SF1-B").Entry.Should().BeNull();

        schematic.Connections.Should().ContainSingle();
        schematic.Connections[0].FixtureId.Should().Be(fixture.Id.Value);
        schematic.Connections[0].MatchNumber.Should().Be(1);
        schematic.Connections[0].SlotAKey.Should().Be("SF1-A");
        schematic.Connections[0].SlotBKey.Should().Be("SF1-B");
        schematic.Connections[0].PairKey.Should().Be("P1");

        // U4: single-pair round → RoundName + Side, no PairOrdinal (Finale-style).
        placed.FormPosition.RoundName.Should().Be("SF");
        placed.FormPosition.RoundOrder.Should().Be(0);
        placed.FormPosition.Side.Should().Be("A");
        placed.FormPosition.PairOrdinal.Should().BeNull();
        schematic.Cases.Single(c => c.FormPosition.SlotKey == "SF1-B").FormPosition.Side.Should().Be("B");
    }

    [Fact]
    public void Cup_slots_without_bracket_pairs_do_not_invent_place_addresses()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Demi-finales", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF-1-A");
        stage.AddSlot("SF-1-B");
        stage.AddSlot("SF-2-A");
        stage.AddSlot("SF-2-B");

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.Connections.Should().BeEmpty();
        schematic.Cases.Should().OnlyContain(c =>
            c.FormPosition.Side == null && c.FormPosition.PairOrdinal == null);
    }

    [Fact]
    public void Cup_multi_pair_round_exposes_pair_ordinal_from_bracket_pairs()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Demi-finale", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.AddSlot("SF2-A");
        stage.AddSlot("SF2-B");
        stage.ReplaceBracketPairs(
        [
            new BracketPair("P1", "SF1-A", "SF1-B"),
            new BracketPair("P2", "SF2-A", "SF2-B")
        ]);
        stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A", "SF1-B", "P1");
        stage.AddFixture(stage.Rounds[0].Id, _clock, "SF2-A", "SF2-B", "P2");

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        var sf1A = schematic.Cases.Single(c => c.FormPosition.SlotKey == "SF1-A");
        sf1A.FormPosition.RoundName.Should().Be("Demi-finale");
        sf1A.FormPosition.PairOrdinal.Should().Be(1);
        sf1A.FormPosition.Side.Should().Be("A");
        sf1A.FormPosition.FixtureId.Should().NotBeNull();

        var sf2B = schematic.Cases.Single(c => c.FormPosition.SlotKey == "SF2-B");
        sf2B.FormPosition.PairOrdinal.Should().Be(2);
        sf2B.FormPosition.Side.Should().Be("B");
        schematic.Connections.Should().HaveCount(2);
        schematic.Connections.Should().OnlyContain(c => c.PairKey != null && c.FixtureId != null);
    }

    [Fact]
    public void Groups_partial_placement_leaves_empty_cases()
    {
        var competition = Competition.Create(new CompetitionName("Groups"), SampleRegulations.Standard(), _clock);
        var e1 = competition.AddEntry(TeamId.New(), "One", _clock);
        var e2 = competition.AddEntry(TeamId.New(), "Two", _clock);
        var e3 = competition.AddEntry(TeamId.New(), "Three", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        stage.SetPlacesPerGroup(4);
        stage.AssignEntryToGroup(groupA.Id, e1.Id);
        stage.AssignEntryToGroup(groupA.Id, e2.Id);
        stage.AssignEntryToGroup(groupA.Id, e3.Id);

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.FormatKind.Should().Be(StructureFormatKind.Groups);
        schematic.Cases.Should().HaveCount(8);
        schematic.Cases.Count(c => c.Entry != null).Should().Be(3);
        schematic.Cases.Count(c => c.Entry == null).Should().Be(5);
        schematic.Cases.Should().OnlyContain(c => c.FeedOrigin == null);
        schematic.Connections.Should().BeEmpty();

        var aPlaces = schematic.Cases
            .Where(c => c.FormPosition.GroupId == groupA.Id.Value)
            .OrderBy(c => c.FormPosition.Index)
            .ToArray();
        aPlaces.Should().HaveCount(4);
        aPlaces[0].Entry!.DisplayName.Should().Be("One");
        aPlaces[1].Entry!.DisplayName.Should().Be("Two");
        aPlaces[2].Entry!.DisplayName.Should().Be("Three");
        aPlaces[3].Entry.Should().BeNull();

        schematic.Cases.Where(c => c.FormPosition.GroupId == groupB.Id.Value)
            .Should()
            .OnlyContain(c => c.Entry == null);
    }

    [Fact]
    public void Groups_inbound_ForGroup_qual_fills_empty_seats_not_title_chrome()
    {
        var competition = Competition.Create(new CompetitionName("A1"), SampleRegulations.Standard(), _clock);
        var source = Stage.Create(competition.Id, new StageName("Poules 1"), SampleRegulations.Standard(), _clock);
        var g1 = source.AddGroup("A", _clock);
        var g2 = source.AddGroup("B", _clock);
        source.AddMatchday(1, _clock);

        var dest = Stage.Create(competition.Id, new StageName("Poules 2"), SampleRegulations.Standard(), _clock);
        var destA = dest.AddGroup("Poule A", _clock);
        dest.AddGroup("Poule B", _clock);
        dest.SetPlacesPerGroup(4);

        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            dest.Id,
            destinationGroupIds: [destA.Id, destA.Id]);
        source.ReplaceQualificationRules(
            QualificationRules.FromIntents([intent], [g1.Id, g2.Id]),
            _clock);

        var schematic = StageSchematicAssembler.Assemble(dest, competition, [source, dest]);

        var destACases = schematic.Cases.Where(c => c.FormPosition.GroupId == destA.Id.Value).ToArray();
        destACases.Should().HaveCount(4);
        destACases.Count(c => c.FeedOrigin is not null).Should().Be(2);
        destACases.Take(2).Should().OnlyContain(c =>
            c.Entry == null
            && c.FeedOrigin != null
            && c.FeedOrigin.Kind == FeedKind.Qualification
            && c.FeedOrigin.SourceStageName == source.Name.Value);
        destACases.Skip(2).Should().OnlyContain(c => c.FeedOrigin == null);
        schematic.GroupFeeds.Should().ContainSingle(f => f.GroupId == destA.Id.Value);
    }

    [Fact]
    public void Championship_places_composition_entries_into_roster_places()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        competition.AddEntry(TeamId.New(), "C", _clock);
        competition.AddEntry(TeamId.New(), "D", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Ligue"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);

        stage.ReplaceAffectationAuthoring(
            [
                competition.Entries[0].Id,
                competition.Entries[1].Id
            ],
            _clock);

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.FormatKind.Should().Be(StructureFormatKind.Championship);
        schematic.Cases.Should().HaveCount(4);
        schematic.Cases[0].Entry!.DisplayName.Should().Be("A");
        schematic.Cases[1].Entry!.DisplayName.Should().Be("B");
        schematic.Cases[2].Entry.Should().BeNull();
        schematic.Cases[3].Entry.Should().BeNull();
        schematic.Connections.Should().BeEmpty();
    }

    [Fact]
    public void Cup_cross_stage_population_progression_does_not_feed_slot_origins()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var source = Stage.Create(competition.Id, new StageName("R32"), SampleRegulations.Standard(), _clock);
        source.AddRound("R32", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("S1");
        source.AddSlot("S2");
        source.AddSlot("S3");
        source.AddSlot("S4");
        source.ReplaceBracketPairs(
        [
            new BracketPair("P1", "S1", "S2"),
            new BracketPair("P2", "S3", "S4")
        ]);
        var early = source.AddFixture(source.Rounds[0].Id, _clock, "S1", "S2", "P1");
        var late = source.AddFixture(source.Rounds[0].Id, _clock, "S3", "S4", "P2");

        var target = Stage.Create(competition.Id, new StageName("R16"), SampleRegulations.Standard(), _clock);
        target.AddRound("R16", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        target.AddSlot("R16-A");
        target.AddSlot("R16-B");
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P2",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(target.Id)),
                new ProgressionPath("P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(target.Id))
            ]),
            _clock);

        var schematic = StageSchematicAssembler.Assemble(target, competition, [source, target]);
        var sourceSchematic = StageSchematicAssembler.Assemble(source, competition, [source, target]);

        schematic.Cases.Should().OnlyContain(c => c.FeedOrigin == null);
        source.Regulation.ProgressionRules!.Paths.Should().HaveCount(2);
        source.Regulation.ProgressionRules.Paths.Should().OnlyContain(p => p.Destination.TargetsPopulation);
        sourceSchematic.Connections.Should().HaveCount(2);
    }

    [Fact]
    public void Cup_fixture_without_slots_and_without_match_is_not_a_connection()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("A");
        stage.AddFixture(stage.Rounds[0].Id, _clock);

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.Connections.Should().BeEmpty();
    }

    [Fact]
    public void Cup_fixture_exposes_sides_and_connection_from_real_match()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var alpha = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var beta = competition.AddEntry(TeamId.New(), "Beta", _clock);
        var stage = Stage.Create(competition.Id, new StageName("R32"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R32", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
        var row = new MatchSummaryRow(matchId, stage.Id, MatchStatus.Finished, alpha.Id, beta.Id, null);

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage], [row]);

        // I3 — Places-first: fixture sides must not replace existing SlotKeys.
        schematic.Cases.Should().HaveCount(2);
        schematic.Cases.Select(c => c.FormPosition.SlotKey).Should().BeEquivalentTo("S1", "S2");
        schematic.Cases.Should().OnlyContain(c => c.Entry == null && c.Assignment == null);

        schematic.Connections.Should().ContainSingle();
        schematic.Connections[0].FixtureId.Should().Be(fixture.Id.Value);
        schematic.Connections[0].SlotAKey.Should().BeNull();
        schematic.Connections[0].SlotBKey.Should().BeNull();
        schematic.Connections[0].MatchNumber.Should().Be(1);
    }

    [Fact]
    public void Cup_slot_draw_applied_assigns_places()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var alpha = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var beta = competition.AddEntry(TeamId.New(), "Beta", _clock);
        var stage = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.ReplaceAffectationAuthoring([alpha.Id, beta.Id], _clock);

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([alpha.Id, beta.Id]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(alpha.Id, "S1"),
                new SlotDrawPlacement(beta.Id, "S2")
            ]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.Cases.Should().HaveCount(2);
        schematic.Cases.Single(c => c.FormPosition.SlotKey == "S1").Assignment!.DisplayName
            .Should().Be("Alpha");
        schematic.Cases.Single(c => c.FormPosition.SlotKey == "S2").Assignment!.DisplayName
            .Should().Be("Beta");
    }

    [Fact]
    public void Cup_slot_draw_exposes_draw_feed_origin_when_published()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var alpha = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var beta = competition.AddEntry(TeamId.New(), "Beta", _clock);
        var stage = Stage.Create(competition.Id, new StageName("R32"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R32", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        stage.ReplaceAffectationAuthoring([alpha.Id, beta.Id], _clock);

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([alpha.Id, beta.Id]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(alpha.Id, "S1"),
                new SlotDrawPlacement(beta.Id, "S2")
            ]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.Cases.Should().HaveCount(2);
        schematic.Cases.Should().OnlyContain(c =>
            c.FeedOrigin != null
            && c.FeedOrigin.Kind == FeedKind.Draw
            && c.FeedOrigin.DrawId == draw.Id.Value);
    }

    [Fact]
    public void Cup_qualification_population_does_not_appear_as_slot_feed()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var source = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var group = source.AddGroup("A", _clock);
        var target = Stage.Create(competition.Id, new StageName("Barrages"), SampleRegulations.Standard(), _clock);
        target.AddRound("BR", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        target.AddSlot("BR-1-A");

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(group.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(target.Id))
            ]),
            _clock);

        var schematic = StageSchematicAssembler.Assemble(target, competition, [source, target]);

        schematic.FormatKind.Should().Be(StructureFormatKind.Cup);
        schematic.Cases.Should().ContainSingle();
        schematic.Cases[0].FeedOrigin.Should().BeNull();
    }

    [Fact]
    public void Swiss_places_composition_into_pool_slots()
    {
        var competition = Competition.Create(new CompetitionName("Swiss"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Suisse"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(roundCount: 5));
        stage.ReplaceAffectationAuthoring(
            [competition.Entries[0].Id, competition.Entries[1].Id],
            _clock);

        var schematic = StageSchematicAssembler.Assemble(stage, competition, [stage]);

        schematic.FormatKind.Should().Be(StructureFormatKind.Swiss);
        schematic.SwissRoundCount.Should().Be(5);
        schematic.Cases.Should().HaveCount(2);
        schematic.Cases[0].Entry!.DisplayName.Should().Be("A");
        schematic.Cases[1].Entry!.DisplayName.Should().Be("B");
    }
}
