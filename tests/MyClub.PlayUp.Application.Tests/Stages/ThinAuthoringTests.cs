// -----------------------------------------------------------------------
// <copyright file="ThinAuthoringTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// D1 thin authoring: AddStage / AddRound / AddSlot / ReplaceProgressionRules composition.
/// </summary>
public sealed class ThinAuthoringTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 26, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Composition_authors_qf_to_sf_chain()
    {
        var competition = CreateCompetition.Execute("Cup-D1", _clock);

        var qf = AddEmptyStage(competition, "Quarter-Finals");
        var qfRound = AddStageRound.Execute(
            qf,
            "QF",
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            _clock);
        AddStageSlot.Execute(qf, "QF1-A");
        AddStageSlot.Execute(qf, "QF1-B");
        var qfFixture = qf.AddFixture(qfRound.Id, _clock, "QF1-A", "QF1-B");

        var sf = AddEmptyStage(competition, "Semi-Finals");
        var sfRound = AddStageRound.Execute(
            sf,
            "SF",
            AddStageRound.BuildTieFormat(numberOfLegs: 2, aggregateScoring: true),
            _clock);
        AddStageSlot.Execute(sf, "SF1-A");
        AddStageSlot.Execute(sf, "SF1-B");

        ReplaceStageProgressionRules.Execute(
            qf,
            [
                new ProgressionPathSpec(
                    qfFixture.Id,
                    ProgressionOutcome.Winner,
                    sf.Id,
                    DestinationSlotKey: null),
                new ProgressionPathSpec(
                    qfFixture.Id,
                    ProgressionOutcome.Loser,
                    sf.Id,
                    DestinationSlotKey: null)
            ],
            _clock);

        competition.StageIds.Should().HaveCount(2);
        competition.StageIds.Should().Contain([qf.Id, sf.Id]);
        qf.Rounds.Should().ContainSingle().Which.Name.Should().Be("QF");
        sfRound.TieFormat.Should().NotBeNull();
        sfRound.TieFormat!.NumberOfLegs.Should().Be(2);
        sfRound.TieFormat.AggregateScoring.Should().BeTrue();
        sf.Slots.Select(s => s.SlotKey).Should().BeEquivalentTo("SF1-A", "SF1-B");
        qf.Regulation.ProgressionRules.Should().NotBeNull();
        qf.Regulation.ProgressionRules!.Paths.Should().HaveCount(2);
        qf.Regulation.ProgressionRules.Paths[0].Destination.StageId.Should().Be(sf.Id);
        qf.Regulation.ProgressionRules.Paths[0].Destination.SlotKey.Should().BeNull();
        qf.Regulation.ProgressionRules.Paths.Should().OnlyContain(p => p.Destination.TargetsPopulation);
    }

    [Fact]
    public void ReplaceStageQualificationRules_authors_path_and_clears()
    {
        var competition = CreateCompetition.Execute("Cup-Qualif", _clock);
        var groups = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2, "Groups"), _clock);
        var ko = AddEmptyStage(competition, "KO");
        AddStageSlot.Execute(ko, "QF1");

        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationPathSpec(
                    1,
                    SelectionMode.Top,
                    2,
                    ko.Id.Value,
                    RankingScope.Overall)
            ],
            _clock);

        groups.Regulation.QualificationRules.Should().NotBeNull();
        groups.Regulation.QualificationRules!.Paths.Should().ContainSingle();
        groups.Regulation.QualificationRules.Paths[0].Destination.StageId.Should().Be(ko.Id);

        ReplaceStageQualificationRules.Execute(groups, (IReadOnlyList<QualificationPathSpec>?)null, _clock);
        groups.Regulation.QualificationRules.Should().BeNull();
    }

    [Fact]
    public void AddCompetitionStage_rejects_when_competition_running()
    {
        var competition = CreateCompetition.Execute("Cup-D1-Run", _clock);
        AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Only"), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var act = () => AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "TooLate"), _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void AddStageRound_rejects_when_stage_running()
    {
        var competition = CreateCompetition.Execute("Cup-D1-Lock", _clock);
        var stage = AddEmptyStage(competition, "Locked");
        AddStageRound.Execute(stage, "R1", null, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => AddStageRound.Execute(stage, "R2", null, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void AddStageSlot_rejects_when_stage_running()
    {
        var competition = CreateCompetition.Execute("Cup-D1-SlotLock", _clock);
        var stage = AddEmptyStage(competition, "Locked");
        AddStageRound.Execute(stage, "R1", null, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => AddStageSlot.Execute(stage, "X");

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void ReplacePlacementAwardRules_authors_final_ranks_without_slots()
    {
        var competition = CreateCompetition.Execute("Cup-Awards", _clock);
        var stage = AddEmptyStage(competition, "Final");
        var round = AddStageRound.Execute(stage, "Final", null, _clock);
        var fixture = stage.AddFixture(round.Id, _clock);

        ReplaceStagePlacementAwardRules.Execute(
            stage,
            [
                new PlacementAwardPathSpec(fixture.Id, ProgressionOutcome.Winner, Rank: 1),
                new PlacementAwardPathSpec(fixture.Id, ProgressionOutcome.Loser, Rank: 2)
            ],
            _clock);

        stage.Regulation.PlacementAwardRules.Should().NotBeNull();
        stage.Regulation.PlacementAwardRules!.Paths.Should().HaveCount(2);
        stage.Regulation.PlacementAwardRules.Paths.Should().Contain(p =>
            p.Outcome == ProgressionOutcome.Winner && p.Rank == 1);
        stage.Regulation.PlacementAwardRules.Paths.Should().Contain(p =>
            p.Outcome == ProgressionOutcome.Loser && p.Rank == 2);
        stage.Slots.Should().BeEmpty();
    }

    [Fact]
    public void ReplacePlacementAwardRules_clears_when_paths_empty()
    {
        var competition = CreateCompetition.Execute("Cup-Awards-Clear", _clock);
        var stage = AddEmptyStage(competition, "Final");
        var round = AddStageRound.Execute(stage, "Final", null, _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        ReplaceStagePlacementAwardRules.Execute(
            stage,
            [
                new PlacementAwardPathSpec(fixture.Id, ProgressionOutcome.Winner, 1)
            ],
            _clock);

        ReplaceStagePlacementAwardRules.Execute(stage, [], _clock);

        stage.Regulation.PlacementAwardRules.Should().BeNull();
    }

    [Fact]
    public void ReplacePlacementAwardRules_rejects_when_stage_running()
    {
        var competition = CreateCompetition.Execute("Cup-Awards-Lock", _clock);
        var stage = AddEmptyStage(competition, "Final");
        var round = AddStageRound.Execute(stage, "R1", null, _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => ReplaceStagePlacementAwardRules.Execute(
            stage,
            [
                new PlacementAwardPathSpec(fixture.Id, ProgressionOutcome.Winner, 1)
            ],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void ReplaceProgressionRules_rejects_when_stage_running()
    {
        var competition = CreateCompetition.Execute("Cup-D1-ProgLock", _clock);
        var stage = AddEmptyStage(competition, "QF");
        var round = AddStageRound.Execute(stage, "R1", null, _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => ReplaceStageProgressionRules.Execute(
            stage,
            [
                new ProgressionPathSpec(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    stage.Id,
                    "A")
            ],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void BuildTieFormat_rejects_invalid_legs()
    {
        var act = () => AddStageRound.BuildTieFormat(3, aggregateScoring: true);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidStructureIntent);
    }

    /// <summary>
    /// Empty Draft stage (identity only) — thin authoring builds rounds/slots itself.
    /// </summary>
    private Stage AddEmptyStage(Competition competition, string name)
    {
        var stage = Stage.Create(
            competition.Id,
            new StageName(name),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            DefaultsBinding.AllBound(isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        return stage;
    }
}
