// -----------------------------------------------------------------------
// <copyright file="StructureViewAssemblerDrawRulesTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Δ-03 — hub must not expose P1 draw-rules authoring on Ready (B-Q1b).
/// </summary>
public sealed class StructureViewAssemblerDrawRulesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_Draft_Cup_exposes_ReplaceDrawRules_when_draw_format()
    {
        var situation = TestCompetition.Create("Draft cup draw action", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.Status.Should().Be(StageStatus.Draft);
        hub.Actions.Should().Contain(StructureViewAssembler.ActionReplaceDrawRules);
    }

    [Fact]
    public void Assemble_Ready_Cup_without_production_omits_ReplaceDrawRules()
    {
        var situation = TestCompetition.Create("Ready cup no P1 hub", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4))
            .PreparePrimaryStage();

        var stage = situation.PrimaryStage!;
        stage.Status.Should().Be(StageStatus.Ready);
        stage.Regulation.DrawRules.Should().BeNull();

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.Actions.Should().NotContain(StructureViewAssembler.ActionReplaceDrawRules);
        hub.Actions.Should().Contain(StructureViewAssembler.ActionReplaceAffectationAuthoring);
    }

    [Fact]
    public void Assemble_Ready_Cup_with_engaged_draw_rules_omits_ReplaceDrawRules()
    {
        var situation = TestCompetition.Create("Ready cup engaged draw", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        StageMaterializationTestSupport.EnsureReadyForMaterialize(
            situation.Competition,
            stage,
            situation.Stages,
            _clock);

        stage.Status.Should().Be(StageStatus.Ready);
        stage.Regulation.DrawRules.Should().NotBeNull();

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.Actions.Should().NotContain(StructureViewAssembler.ActionReplaceDrawRules);
    }

    [Fact]
    public void Assemble_Ready_Groups_without_production_omits_ReplaceDrawRules()
    {
        var situation = TestCompetition.Create("Ready groups no P1 hub", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2))
            .PreparePrimaryStage();

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.Status.Should().Be(StageStatus.Ready);
        hub.Actions.Should().NotContain(StructureViewAssembler.ActionReplaceDrawRules);
    }
}
