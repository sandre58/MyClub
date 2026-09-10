// -----------------------------------------------------------------------
// <copyright file="StructureConfrontationSegmentsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureConfrontationSegmentsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_groups_consecutive_rounds_with_same_tie_format()
    {
        var competition = CreateCompetition.Execute("CL Style", _clock);
        var twoLegs = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: new AwayGoalsRule(),
            extraTimeRule: new ExtraTimeRule(),
            penaltyShootoutRule: new PenaltyShootoutRule());
        var oneLeg = new TieFormat(TieFormat.SingleLeg, aggregateScoring: false);

        var stage = Stage.Create(
            competition.Id,
            new StageName("Phase finale"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        stage.AddRound("Quarts de finale", twoLegs, _clock);
        stage.AddRound("Demis de finale", twoLegs, _clock);
        stage.AddRound("Finale", oneLeg, _clock);
        competition.AddStage(stage.Id, _clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Should().ContainSingle().Subject;

        hub.HasTieFormat.Should().BeTrue();
        hub.ConfrontationSegments.Should().NotBeNull();
        hub.ConfrontationSegments!.Should().HaveCount(2);

        var first = hub.ConfrontationSegments[0];
        first.Rounds.Select(round => round.Name).Should().Equal("Quarts de finale", "Demis de finale");
        first.NumberOfLegs.Should().Be(2);
        first.AggregateScoring.Should().BeTrue();
        first.HasAwayGoalsRule.Should().BeTrue();
        first.HasTieExtraTime.Should().BeTrue();
        first.HasTiePenaltyShootout.Should().BeTrue();

        var second = hub.ConfrontationSegments[1];
        second.Rounds.Select(round => round.Name).Should().Equal("Finale");
        second.NumberOfLegs.Should().Be(1);
        second.AggregateScoring.Should().BeFalse();
        second.HasAwayGoalsRule.Should().BeFalse();

        hub.NumberOfLegs.Should().Be(2);
        hub.AggregateScoring.Should().BeTrue();
    }

    [Fact]
    public void Assemble_single_segment_when_all_rounds_share_format()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var oneLeg = new TieFormat(TieFormat.SingleLeg, aggregateScoring: false);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Coupe"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        stage.AddRound("Quarts", oneLeg, _clock);
        stage.AddRound("Demis", oneLeg, _clock);
        stage.AddRound("Finale", oneLeg, _clock);
        competition.AddStage(stage.Id, _clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();

        hub.HasTieFormat.Should().BeTrue();
        hub.ConfrontationSegments.Should().ContainSingle();
        hub.ConfrontationSegments![0].Rounds.Should().HaveCount(3);
        hub.NumberOfLegs.Should().Be(1);
    }

    [Fact]
    public void Assemble_omits_segments_when_no_stored_tie_format()
    {
        var competition = CreateCompetition.Execute("Champ", _clock);
        var result = ConfigureStructure.Execute(
            competition,
            primaryStage: null,
            StructureIntent.Championship(3),
            _clock);

        var hub = StructureViewAssembler.Assemble(competition, [result.Stage]).Stages.Single();

        hub.HasTieFormat.Should().BeFalse();
        hub.ConfrontationSegments.Should().BeNull();
        hub.NumberOfLegs.Should().BeNull();
    }
}
