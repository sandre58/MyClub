// -----------------------------------------------------------------------
// <copyright file="StageFixtureSlotsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages.Fixtures;

public sealed class StageFixtureSlotsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 17, 30, 0, TimeSpan.Zero));

    [Fact]
    public void AddFixture_with_slot_keys_binds_positions()
    {
        var stage = CreateCupWithSlots();
        var round = stage.Rounds[0];

        var fixture = stage.AddFixture(round.Id, _clock, "QF1-A", "QF1-B");

        fixture.SlotAKey.Should().Be("QF1-A");
        fixture.SlotBKey.Should().Be("QF1-B");
    }

    [Fact]
    public void AddFixture_rejects_unknown_slot()
    {
        var stage = CreateCupWithSlots();
        var round = stage.Rounds[0];

        var act = () => stage.AddFixture(round.Id, _clock, "QF1-A", "Missing");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotNotFound);
    }

    [Fact]
    public void ReplaceFixtureSlots_updates_keys()
    {
        var stage = CreateCupWithSlots();
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);

        stage.ReplaceFixtureSlots(fixture.Id, "QF1-A", "QF1-B");

        stage.GetFixture(fixture.Id).SlotAKey.Should().Be("QF1-A");
        stage.GetFixture(fixture.Id).SlotBKey.Should().Be("QF1-B");
    }

    [Fact]
    public void ReplaceFixtureSlots_demotes_ready_to_draft()
    {
        var stage = CreateCupWithSlots();
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);

        stage.ReplaceFixtureSlots(fixture.Id, "QF1-A", "QF1-B");

        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Fixture_rejects_identical_slot_keys()
    {
        var stage = CreateCupWithSlots();

        var act = () => stage.AddFixture(stage.Rounds[0].Id, _clock, "QF1-A", "QF1-A");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void RemoveSlot_blocked_when_referenced_by_fixture()
    {
        var stage = CreateCupWithSlots();
        stage.AddFixture(stage.Rounds[0].Id, _clock, "QF1-A", "QF1-B");

        var act = () => stage.RemoveSlot("QF1-A");

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotReferenced);
    }

    private Stage CreateCupWithSlots()
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
