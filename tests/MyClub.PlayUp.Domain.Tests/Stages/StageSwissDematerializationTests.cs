// -----------------------------------------------------------------------
// <copyright file="StageSwissDematerializationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageSwissDematerializationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public void RemoveSwissRoundMaterialization_while_running_removes_matchday_fixtures_and_bye()
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(competitionId, new StageName("Swiss"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(3));
        stage.Prepare(_clock);
        stage.Start(_clock);

        var matchday = stage.AddSwissRoundMatchday(_clock);
        var fixture = stage.AddSwissRoundFixture(matchday.Id, _clock);
        var match = Match.Create(competitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);
        stage.AttachSwissRoundMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.RecordSwissBye(matchday.Number, EntryId.New());
        stage.ClearDomainEvents();

        var removed = stage.RemoveSwissRoundMaterialization(matchday.Number, _clock);

        removed.Should().ContainSingle().Which.Should().Be(match.Id);
        stage.HasMatch(match.Id).Should().BeFalse();
        stage.Matchdays.Should().BeEmpty();
        stage.SwissByeHistory.Should().BeEmpty();
        stage.DomainEvents.Should().Contain(e => e is StageFixtureRemoved);
        stage.DomainEvents.Should().Contain(e => e is StageMatchdayRemoved);
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void RemoveSwissRoundMaterialization_rejects_when_stage_completed()
    {
        var stage = Stage.Create(CompetitionId.New(), new StageName("Swiss"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(2));
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);

        var act = () => stage.RemoveSwissRoundMaterialization(1, _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }
}
