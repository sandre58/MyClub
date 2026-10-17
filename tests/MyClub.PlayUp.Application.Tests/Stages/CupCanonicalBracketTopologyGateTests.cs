// -----------------------------------------------------------------------
// <copyright file="CupCanonicalBracketTopologyGateTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class CupCanonicalBracketTopologyGateTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void MaterializeCupFromOccupiedSlots_repairs_missing_pairs_in_Ready()
    {
        var competition = CreateCompetition.Execute("Cup-repair", _clock);
        var stage = CreateCanonicalCupDraft(competition.Id, 4, _clock);
        stage.AppendCanonicalBracketPairs([new BracketPair("P1", "S1", "S2")]);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.BracketPairs.Should().ContainSingle();

        var home = EntryId.New();
        var away = EntryId.New();
        stage.ApplyResolvedEntry("S1", home, _clock);
        stage.ApplyResolvedEntry("S2", away, _clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(competition, stage, ["P1"], [], _clock);

        result.CreatedMatches.Should().HaveCount(1);
        stage.BracketPairs.Should().HaveCount(2);
    }

    [Fact]
    public void MaterializeCupFromOccupiedSlots_does_not_repair_explicit_topology()
    {
        var competition = CreateCompetition.Execute("Cup-explicit", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Tour", _clock);
        stage.AddSlot("QF-1-A");
        stage.AddSlot("QF-1-B");
        stage.AddSlot("F-A");
        stage.AddSlot("F-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "F-A", "F-B")]);
        stage.ApplyResolvedEntry("F-A", EntryId.New(), _clock);
        stage.ApplyResolvedEntry("F-B", EntryId.New(), _clock);
        stage.Prepare(_clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(competition, stage, ["P1"], [], _clock);

        result.CreatedMatches.Should().HaveCount(1);
        stage.BracketPairs.Should().ContainSingle();
    }

    [Fact]
    public void PrepareStage_refuses_InvalidCanonicalShape()
    {
        var stage = CreateCanonicalCupDraft(CompetitionId.New(), 15, _clock);

        var act = () => PrepareStage.Execute(stage, [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.CupCanonicalTopologyInvalid);
        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void PrepareStage_completes_missing_pairs_before_Ready()
    {
        var stage = CreateCanonicalCupDraft(CompetitionId.New(), 4, _clock);

        PrepareStage.Execute(stage, [stage], _clock);

        stage.Status.Should().Be(StageStatus.Ready);
        stage.BracketPairs.Should().HaveCount(2);
    }

    private static Stage CreateCanonicalCupDraft(CompetitionId competitionId, int slotCount, IClock clock)
    {
        var stage = Stage.Create(competitionId, new StageName("Cup"), SampleRegulations.Standard(), clock);
        stage.AddRound("Tour", clock);
        for (var index = 1; index <= slotCount; index++)
        {
            stage.AddSlot($"S{index}");
        }

        return stage;
    }
}
