// -----------------------------------------------------------------------
// <copyright file="StructureViewAssemblerConfiguredMatchdayCountTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureViewAssemblerConfiguredMatchdayCountTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ConfiguredMatchdayCount_groups_technical_seed_only_is_zero()
    {
        var situation = TestCompetition.Create("Groups", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(groupCount: 2, participantsPerGroup: 2));
        var stage = situation.RequirePrimaryStage();

        stage.Matchdays.Count.Should().Be(1);

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages.Single();

        hub.ConfiguredMatchdayCount.Should().Be(0);
        hub.MatchdayCount.Should().Be(0);
    }

    [Fact]
    public void ConfiguredMatchdayCount_championship_ready_skeleton_without_attached_matches()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages.Single();

        stage.Matchdays.Count.Should().Be(1);
        hub.ConfiguredMatchdayCount.Should().Be(0);
        hub.MatchdayCount.Should().Be(0);
    }

    [Fact]
    public void ConfiguredMatchdayCount_five_configured_two_with_attached_matches()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        EnsureConfiguredMatchdayCount(stage, 5, _clock);

        var entryIds = situation.Competition.Entries.Select(e => e.Id).ToArray();
        AttachMatchOnMatchday(stage, situation.Competition, stage.Matchdays[0], entryIds[0], entryIds[1]);
        AttachMatchOnMatchday(stage, situation.Competition, stage.Matchdays[1], entryIds[2], entryIds[3]);
        situation.PreparePrimaryStageForMaterialization();

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages.Single();

        hub.ConfiguredMatchdayCount.Should().Be(5);
        hub.MatchdayCount.Should().Be(2);
    }

    [Fact]
    public void ConfiguredMatchdayCount_zero_when_no_matchdays_on_skeleton()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        _ = AddEntry.Execute(competition, "A", _clock);
        _ = AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(bracketSize: 4),
            _clock);
        var stage = configured.Stage;
        stage.Matchdays.Should().BeEmpty();

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();

        hub.ConfiguredMatchdayCount.Should().Be(0);
        hub.MatchdayCount.Should().Be(0);
    }

    [Fact]
    public void ConfiguredMatchdayCount_per_stage_not_primary_topology()
    {
        var competition = CreateCompetition.Execute("Form partial", _clock);
        var source = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(source.Id, _clock);
        _ = source.AddGroup("A", _clock);
        _ = source.AddGroup("B", _clock);
        source.AddMatchday(1, _clock);
        source.AddMatchday(2, _clock);

        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        champ.AddMatchday(2, _clock);
        champ.AddMatchday(3, _clock);
        competition.AddStage(champ.Id, _clock);

        var view = StructureViewAssembler.Assemble(competition, [source, champ]);

        var sourceHub = view.Stages.Single(s => s.StageId == source.Id.Value);
        var champHub = view.Stages.Single(s => s.StageId == champ.Id.Value);

        sourceHub.ConfiguredMatchdayCount.Should().Be(2);
        champHub.ConfiguredMatchdayCount.Should().Be(3);
        sourceHub.ConfiguredMatchdayCount.Should().NotBe(champHub.ConfiguredMatchdayCount);
        view.Structure.MatchdayCount.Should().Be(source.Matchdays.Count);
    }

    private static void EnsureConfiguredMatchdayCount(Stage stage, int target, IClock clock)
    {
        var nextNumber = stage.Matchdays.Count == 0
            ? 1
            : stage.Matchdays.Max(m => m.Number) + 1;
        while (stage.Matchdays.Count < target)
        {
            stage.AddMatchday(nextNumber, clock);
            nextNumber++;
        }
    }

    private void AttachMatchOnMatchday(
        Stage stage,
        Competition competition,
        Matchday matchday,
        EntryId home,
        EntryId away)
    {
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
    }
}
