// -----------------------------------------------------------------------
// <copyright file="StageRevertLifecycleTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageRevertLifecycleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void RevertToDraft_from_Ready_sets_Draft_and_raises_event()
    {
        var stage = CreateReadyChampionship();
        stage.ClearDomainEvents();

        stage.RevertToDraft(_clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle(e => e is StageRevertedToDraft);
    }

    [Fact]
    public void RevertToDraft_from_Draft_throws()
    {
        var stage = CreateDraftChampionship();

        var act = () => stage.RevertToDraft(_clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RevertToReady_from_Running_sets_Ready_and_raises_event()
    {
        var stage = CreateReadyChampionship();
        stage.Start(_clock);
        stage.ClearDomainEvents();

        stage.RevertToReady(_clock);

        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.OfType<StageRevertedToReady>().Should().ContainSingle();
    }

    [Fact]
    public void RevertToReady_from_Suspended_sets_Ready()
    {
        var stage = CreateReadyChampionship();
        stage.Start(_clock);
        stage.Suspend(_clock);

        stage.RevertToReady(_clock);

        stage.Status.Should().Be(StageStatus.Ready);
    }

    private Stage CreateReadyChampionship()
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        return stage;
    }

    private Stage CreateDraftChampionship()
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        return stage;
    }
}
