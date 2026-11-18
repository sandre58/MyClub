// -----------------------------------------------------------------------
// <copyright file="GroupDrawPotsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class GroupDrawPotsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Compute_fills_eight_entries_across_four_pots_in_composition_order()
    {
        var ids = Enumerable.Range(0, 8).Select(_ => EntryId.New()).ToArray();

        var pots = GroupDrawPots.Compute(ids, 4);

        ids.Select(id => pots[id]).Should().Equal(1, 1, 2, 2, 3, 3, 4, 4);
    }

    [Fact]
    public void Compute_rejects_a_count_that_is_not_divisible_with_the_draw_failure()
    {
        var ids = Enumerable.Range(0, 8).Select(_ => EntryId.New()).ToArray();

        var act = () => GroupDrawPots.Compute(ids, 3);

        var failure = act.Should().Throw<ApplicationFailureException>().Which;
        failure.Code.Should().Be(ApplicationErrorCodes.DrawGenerationFailure);
        failure.Message.Should().Be("Entry pool count (8) must be divisible by NumberOfPots (3).");
    }

    [Fact]
    public void TryRead_returns_null_without_draw_rules_pot_rules_composition_or_a_divisible_count()
    {
        var withoutRules = GroupsStage(8);
        GroupDrawPots.TryRead(withoutRules).Should().BeNull();

        var withoutPots = GroupsStage(8);
        ReplaceStageDrawRules.Execute(withoutPots, new DrawRules(DrawMode.Random), _clock);
        GroupDrawPots.TryRead(withoutPots).Should().BeNull();

        var empty = GroupsStage(0);
        ReplaceStageDrawRules.Execute(
            empty,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        GroupDrawPots.TryRead(empty).Should().BeNull();

        var indivisible = GroupsStage(8);
        ReplaceStageDrawRules.Execute(
            indivisible,
            new DrawRules(DrawMode.Random, potRules: new PotRules(3)),
            _clock);
        GroupDrawPots.TryRead(indivisible).Should().BeNull();
    }

    [Fact]
    public void CreateDefault_group_pot_membership_matches_the_shared_function()
    {
        var stage = GroupsStage(8);
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(4)),
            _clock);
        var ids = stage.CompositionEntries.Select(entry => entry.EntryId).ToArray();

        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group);

        inputs.PotMembership.Should().NotBeNull();
        inputs.PotMembership!.Pots.Should().BeEquivalentTo(GroupDrawPots.Compute(ids, 4));
        ids.Select(id => inputs.PotMembership.Pots[id]).Should().Equal(1, 1, 2, 2, 3, 3, 4, 4);
    }

    [Fact]
    public void CreateDefault_group_without_pot_rules_keeps_the_factory_message()
    {
        var stage = GroupsStage(4);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);

        var act = () => DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group);

        var failure = act.Should().Throw<ApplicationFailureException>().Which;
        failure.Code.Should().Be(ApplicationErrorCodes.DrawGenerationFailure);
        failure.Message.Should().Be("Group draw requires PotRules on the stage regulation.");
    }

    private Stage GroupsStage(int compositionCount)
    {
        var competition = CreateCompetition.Execute($"Pots {compositionCount} {_clock.UtcNow.Ticks}", _clock);
        var ids = new List<EntryId>();
        for (var index = 0; index < Math.Max(compositionCount, 2); index++)
        {
            ids.Add(AddEntry.Execute(competition, $"T{index}", _clock).Id);
        }

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        if (compositionCount > 0)
        {
            stage.ReplaceAffectationAuthoring(ids.Take(compositionCount).ToArray(), _clock);
        }

        return stage;
    }
}
