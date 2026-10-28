// -----------------------------------------------------------------------
// <copyright file="StructureViewAssemblerQualProgProjectionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureViewAssemblerQualProgProjectionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_projects_qualification_intents_when_rules_use_intent_authoring()
    {
        var situation = TestCompetition.Create("Qual intents", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4, "Poules"));
        var groups = situation.PrimaryStage!;
        var league = situation.AddBareStage("League");
        league.AddMatchday(1, _clock);

        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    2,
                    league.Id.Value)
            ],
            _clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages)
            .Stages.Single(s => s.StageId == groups.Id.Value);

        hub.QualificationIntents.Should().NotBeNull().And.NotBeEmpty();
        hub.QualificationPathCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Assemble_synthesizes_qualification_intents_from_legacy_path_list()
    {
        var competition = CreateCompetition.Execute("Path only", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);

        var source = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(source.Id, _clock);
        source.AddGroup("A", _clock);
        source.AddMatchday(1, _clock);

        var ghostStageId = StageId.New();
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 2),
                    QualificationDestination.ForPopulation(ghostStageId))
            ]),
            _clock);

        var hub = StructureViewAssembler.Assemble(competition, [source]).Stages.Single();

        hub.QualificationIntents.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public void Assemble_projects_progression_intents_when_rules_use_intent_authoring()
    {
        var competition = CreateCompetition.Execute("Prog intents", _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(destination.Id, _clock);

        source.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("S1");
        source.AddSlot("S2");
        source.AddSlot("S3");
        source.AddSlot("S4");
        source.SeedEntryRoundBracketPairs();

        ReplaceStageProgressionRules.Execute(
            source,
            [
                new ProgressionIntentSpec(
                    IntentId: null,
                    Order: 1,
                    RoundId: source.Rounds[0].Id,
                    Outcome: ProgressionOutcome.Winner,
                    DestinationStageId: destination.Id)
            ],
            _clock);

        var hub = StructureViewAssembler.Assemble(competition, [source, destination])
            .Stages.Single(s => s.StageId == source.Id.Value);

        hub.ProgressionIntents.Should().NotBeNull().And.NotBeEmpty();
        hub.ProgressionPathCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Assemble_coalesces_legacy_group_winner_paths_with_distinct_match_numbers_into_one_intent()
    {
        var competition = CreateCompetition.Execute("Flux — Prog Groupe", _clock);
        var source = Stage.Create(competition.Id, new StageName("Demi-finales"), SampleRegulations.Standard(), _clock);
        var groups = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(groups.Id, _clock);

        var round = source.AddRound("Tour principal", SampleRegulations.KnockoutTieDecidable(), _clock);
        source.AddSlot("SF1-A");
        source.AddSlot("SF1-B");
        source.AddSlot("SF2-A");
        source.AddSlot("SF2-B");
        source.ReplaceBracketPairs(
        [
            new BracketPair("P1", "SF1-A", "SF1-B"),
            new BracketPair("P2", "SF2-A", "SF2-B")
        ]);
        source.AddFixture(round.Id, _clock, "SF2-A", "SF2-B", "P2");
        source.AddFixture(round.Id, _clock, "SF1-A", "SF1-B", "P1");

        var groupA = groups.AddGroup("A", _clock);
        var groupB = groups.AddGroup("B", _clock);
        groups.AddMatchday(1, _clock);

        // Authored P2 then P1. Expand order is PairKey (P1, P2), which is also Match #1 then Match #2.
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P2",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForGroup(groups.Id, groupB.Id)),
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForGroup(groups.Id, groupA.Id))
            ]),
            _clock);

        var schematic = StageSchematicAssembler.Assemble(source, competition, [source, groups]);
        var matchByPair = schematic.Connections.ToDictionary(connection => connection.PairKey!);
        matchByPair["P1"].MatchNumber.Should().Be(1);
        matchByPair["P2"].MatchNumber.Should().Be(2);

        var hub = StructureViewAssembler.Assemble(competition, [source, groups])
            .Stages.Single(stage => stage.StageId == source.Id.Value);

        hub.ProgressionIntents.Should().ContainSingle();
        var intent = hub.ProgressionIntents!.Single();
        intent.Outcome.Should().Be(ProgressionOutcome.Winner);
        intent.RoundName.Should().Be("Tour principal");
        intent.DestinationGroupIds.Should().Equal(groupA.Id.Value, groupB.Id.Value);
        intent.DestinationSlotKeys.Should().BeNull();
        intent.ExpandedPathCount.Should().Be(2);
        hub.StructureIssues.Should().NotContain(StructureViewAssembler.IssueDuplicateProgressionRoundOutcome);
        hub.PresentedIssueCodes.Should().NotContain(StructureViewAssembler.IssueDuplicateProgressionRoundOutcome);
    }

    [Fact]
    public void Assemble_still_coalesces_legacy_slot_winner_paths_across_distinct_match_numbers()
    {
        var competition = CreateCompetition.Execute("Prog slots", _clock);
        var source = Stage.Create(competition.Id, new StageName("Demi-finales"), SampleRegulations.Standard(), _clock);
        var final = Stage.Create(competition.Id, new StageName("Finale"), SampleRegulations.Standard(), _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(final.Id, _clock);

        var round = source.AddRound("Tour principal", SampleRegulations.KnockoutTieDecidable(), _clock);
        source.AddSlot("SF1-A");
        source.AddSlot("SF1-B");
        source.AddSlot("SF2-A");
        source.AddSlot("SF2-B");
        source.ReplaceBracketPairs(
        [
            new BracketPair("P1", "SF1-A", "SF1-B"),
            new BracketPair("P2", "SF2-A", "SF2-B")
        ]);
        source.AddFixture(round.Id, _clock, "SF1-A", "SF1-B", "P1");
        source.AddFixture(round.Id, _clock, "SF2-A", "SF2-B", "P2");
        final.AddSlot("F-A");
        final.AddSlot("F-B");

        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P2",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForSlot(final.Id, "F-B")),
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForSlot(final.Id, "F-A"))
            ]),
            _clock);

        var hub = StructureViewAssembler.Assemble(competition, [source, final])
            .Stages.Single(stage => stage.StageId == source.Id.Value);

        hub.ProgressionIntents.Should().ContainSingle();
        var intent = hub.ProgressionIntents!.Single();
        intent.DestinationSlotKeys.Should().Equal("F-A", "F-B");
        intent.DestinationGroupIds.Should().BeNull();
        intent.ExpandedPathCount.Should().Be(2);
        hub.StructureIssues.Should().NotContain(StructureViewAssembler.IssueDuplicateProgressionRoundOutcome);
    }
}
