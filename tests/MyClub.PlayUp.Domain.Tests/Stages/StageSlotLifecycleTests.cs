// -----------------------------------------------------------------------
// <copyright file="StageSlotLifecycleTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageSlotLifecycleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Prepare_allows_slot_with_progression_feed_and_null_entry()
    {
        var stage = CreatePositionalKnockout();
        var qf = stage.Rounds[0];
        var fixture = stage.AddFixture(qf.Id, _clock, "QF1-A", "QF1-B");
        stage.AddSlot("SF1-A");
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        stage.Prepare(_clock);

        stage.Status.Should().Be(StageStatus.Ready);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Prepare_allows_slot_without_local_feed_visibility_gap()
    {
        var stage = CreatePositionalKnockout();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "QF1-A", "QF1-B");

        var act = () => stage.Prepare(_clock);

        act.Should().NotThrow();
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Start_positional_requires_playable_fixture_in_initial_round()
    {
        var stage = CreatePositionalKnockout();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "QF1-A", "QF1-B");
        stage.Prepare(_clock);

        var act = () => stage.Start(_clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.NotReady);
    }

    [Fact]
    public void Start_positional_succeeds_when_initial_round_fixture_resolved()
    {
        var stage = CreatePositionalKnockout();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "QF1-A", "QF1-B");
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ReplaceCompositionEntries([a, b], _clock);
        stage.AssignEntryToSlot("QF1-A", a);
        stage.AssignEntryToSlot("QF1-B", b);
        stage.Prepare(_clock);

        stage.Start(_clock);

        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Start_rejects_when_only_later_round_is_resolved()
    {
        var stage = CreatePositionalKnockout();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "QF1-A", "QF1-B");
        var sf = stage.AddRound("SF", _clock);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.AddFixture(sf.Id, _clock, "SF1-A", "SF1-B");
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ReplaceCompositionEntries([a, b], _clock);
        stage.AssignEntryToSlot("SF1-A", a);
        stage.AssignEntryToSlot("SF1-B", b);
        stage.Prepare(_clock);

        var act = () => stage.Start(_clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.NotReady);
    }

    [Fact]
    public void Cup_without_slot_keys_can_start_when_ready()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("QF", _clock);
        stage.Prepare(_clock);

        stage.Start(_clock);

        stage.Status.Should().Be(StageStatus.Running);
    }

    private Stage CreatePositionalKnockout()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("QF", _clock);
        stage.AddSlot("QF1-A");
        stage.AddSlot("QF1-B");
        return stage;
    }
}
