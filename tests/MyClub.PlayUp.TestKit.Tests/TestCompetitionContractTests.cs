// -----------------------------------------------------------------------
// <copyright file="TestCompetitionContractTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.TestKit.Tests;

/// <summary>
/// Locks TestKit helper contracts: App-gated Prepare and documented Domain shortcuts.
/// </summary>
public sealed class TestCompetitionContractTests
{
    private static readonly DateTimeOffset Epoch =
        new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PreparePrimaryStage_moves_championship_to_Ready()
    {
        var situation = TestCompetition.Create("PrepReady", new FixedClock(Epoch))
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();

        situation.RequirePrimaryStage().Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void PreparePrimaryStage_then_StartPrimaryStage_reaches_Running()
    {
        var situation = TestCompetition.Create("PrepStart", new FixedClock(Epoch))
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage()
            .StartPrimaryStage();

        situation.RequirePrimaryStage().Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void CreateResolvedSlotDraw_leaves_draw_Draft_with_Resolved_resolution()
    {
        var clock = new FixedClock(Epoch);
        var situation = TestCompetition.Create("ResolvedDraw", clock)
            .WithTeams(2);
        var stage = situation.AddKnockoutStage(
            "KO",
            "R16",
            ["S1", "S2"],
            seedBracketPairs: false);
        var pool = situation.Competition.Entries.Select(e => e.Id).ToArray();

        var drawId = situation.CreateResolvedSlotDraw(stage, pool, ["S1", "S2"]);

        var draw = stage.GetDraw(drawId);
        draw.Status.Should().Be(DrawStatus.Draft);
        draw.Kind.Should().Be(DrawResolutionKind.Slot);
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        draw.Resolution.SlotResults.Should().HaveCount(2);
    }

    [Fact]
    public void AddKnockoutStage_seeds_slots_and_optional_bracket_pairs()
    {
        var situation = TestCompetition.Create("KO", new FixedClock(Epoch));
        var stage = situation.AddKnockoutStage(
            "Finals",
            "QF",
            ["A", "B", "C", "D"],
            seedBracketPairs: true);

        stage.Slots.Should().HaveCount(4);
        stage.Rounds.Should().ContainSingle(r => r.Name == "QF");
        stage.BracketPairs.Should().HaveCount(2);
        situation.Stages.Should().ContainSingle(s => s.Id.Equals(stage.Id));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
