// -----------------------------------------------------------------------
// <copyright file="RemoveCompetitionStageTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

/// <summary>
/// Structure Lot 2: remove stage + scrub inbound Qualif/Prog dependencies.
/// </summary>
public sealed class RemoveCompetitionStageTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 10, 11, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_scrubs_peer_progression_paths_targeting_removed_stage()
    {
        var competition = CreateCompetition.Execute("Cup-Remove", _clock);
        var qf = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Quarter-Finals"), _clock);
        var qfRound = AddStageRound.Execute(qf, "QF", null, _clock);
        AddStageSlot.Execute(qf, "QF1-A");
        AddStageSlot.Execute(qf, "QF1-B");
        var fixture = qf.AddFixture(qfRound.Id, _clock, "QF1-A", "QF1-B");

        var sf = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Semi-Finals"), _clock);
        AddStageSlot.Execute(sf, "SF1-A");

        ReplaceStageProgressionRules.Execute(
            qf,
            [
                new ProgressionPathSpec(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    sf.Id,
                    DestinationSlotKey: null)
            ],
            _clock);

        var impact = RemoveCompetitionStage.Execute(competition, sf, [qf, sf], _clock);

        impact.ScrubbedProgressionPaths.Should().Be(1);
        competition.StageIds.Should().Equal(qf.Id);
        qf.Regulation.ProgressionRules.Should().BeNull();
    }

    [Fact]
    public void Execute_scrubs_peer_qualification_paths_targeting_removed_stage()
    {
        var competition = CreateCompetition.Execute("Groups-Remove", _clock);
        var groups = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2, "Groups"), _clock);
        var knockout = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Knockout"), _clock);
        AddStageSlot.Execute(knockout, "QF1");

        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationPathSpec(
                    Order: 1,
                    SelectionMode.Position,
                    SelectionValue: 1,
                    knockout.Id.Value,
                    RankingScope.Overall)
            ],
            _clock);

        var impact = RemoveCompetitionStage.Execute(competition, knockout, [groups, knockout], _clock);

        impact.ScrubbedQualificationPaths.Should().Be(1);
        groups.Regulation.QualificationRules.Should().BeNull();
        competition.StageIds.Should().Equal(groups.Id);
    }

    [Fact]
    public void Execute_rejects_removing_last_stage()
    {
        var competition = CreateCompetition.Execute("Solo", _clock);
        var only = AddCompetitionStage.Execute(competition, StructureIntent.Cup(2, "Only"), _clock);

        var act = () => RemoveCompetitionStage.Execute(competition, only, [only], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.LastStageCannotBeRemoved);
    }
}
