// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlotsRMatM2bTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// <see cref="CompetitionFlowAnalyzer.NonRootUnfed"/> on a foreign phase
/// must not block <see cref="MaterializeCupFromOccupiedSlots"/> on an independent eligible batch.
/// Guards against a blanket topology veto.
/// </summary>
public sealed class MaterializeCupFromOccupiedSlotsRMatM2BTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_materializes_S_when_foreign_leaf_T_emits_NonRootUnfed_without_batch_feed_dependency()
    {
        var competition = CreateCompetition.Execute("R-Mat-M2b", _clock);

        var root = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            SampleRegulations.Standard(),
            _clock);
        competition.AddStage(root.Id, _clock);
        root.AddGroup("A", _clock);
        root.AddMatchday(1, _clock);

        var s = CreateSemiCupStage(competition.Id);
        competition.AddStage(s.Id, _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        s.ApplyResolvedEntry("SF1-A", home, _clock);
        s.ApplyResolvedEntry("SF1-B", away, _clock);
        EnsureCupReady(s);

        var t = Stage.Create(
            competition.Id,
            new StageName("Finale"),
            SampleRegulations.Standard(),
            _clock);
        competition.AddStage(t.Id, _clock);
        t.AddRound("Finale", tieFormat: null, _clock);
        t.AddSlot("F-A");
        t.AddSlot("F-B");
        t.ReplaceBracketPairs([new BracketPair("F1", "F-A", "F-B")]);

        var stages = new[] { root, s, t };
        var topology = CompetitionFlowAnalyzer.AnalyzeTopology(stages, root.Id);
        topology.Should().ContainSingle(d =>
            d.Code == CompetitionFlowAnalyzer.NonRootUnfed && d.StageId == t.Id.Value);

        var hasInboundToT = stages.Any(other =>
            other.Id != t.Id &&
            (other.Regulation.QualificationRules?.Paths.Any(p => p.Destination.StageId.Equals(t.Id)) == true
             || other.Regulation.ProgressionRules?.Paths.Any(p => p.Destination.StageId.Equals(t.Id)) == true));
        hasInboundToT.Should().BeFalse("batch on S must not depend on feed from foreign T");

        var fixtureCountBefore = s.Rounds.Sum(round => round.Fixtures.Count);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            s,
            ["P1"],
            [],
            _clock);

        result.AlreadyComplete.Should().BeFalse();
        result.CreatedMatches.Should().ContainSingle();
        result.CreatedMatches[0].HomeEntryId.Should().Be(home);
        result.CreatedMatches[0].AwayEntryId.Should().Be(away);
        s.Rounds.Sum(round => round.Fixtures.Count).Should().Be(fixtureCountBefore + 1);
        s.FindFixtureByBracketPairKey("P1").Should().NotBeNull();
        t.Rounds.Sum(round => round.Fixtures.Count).Should().Be(0);
    }

    private Stage CreateSemiCupStage(CompetitionId competitionId)
    {
        var stage = Stage.Create(competitionId, new StageName("Demi"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Demi-finales", tieFormat: null, _clock);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.AddSlot("SF2-A");
        stage.AddSlot("SF2-B");
        stage.SeedEntryRoundBracketPairs();
        return stage;
    }

    private void EnsureCupReady(Stage stage)
    {
        if (stage.Status is not StageStatus.Ready)
        {
            stage.Prepare(_clock);
        }
    }
}
