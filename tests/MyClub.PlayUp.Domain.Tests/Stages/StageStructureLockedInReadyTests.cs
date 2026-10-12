// -----------------------------------------------------------------------
// <copyright file="StageStructureLockedInReadyTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageStructureLockedInReadyTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void AddMatchday_in_Ready_is_rejected_and_status_unchanged()
    {
        var stage = CreateReadyChampionship();
        stage.ClearDomainEvents();

        var act = () => stage.AddMatchday(2, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Ready);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceMatchRules_in_Ready_is_rejected()
    {
        var stage = CreateReadyChampionship();
        var rules = SampleRegulations.Standard().MatchRules;

        var act = () => stage.ReplaceMatchRules(rules, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Rename_in_Ready_is_allowed()
    {
        var stage = CreateReadyChampionship();

        stage.Rename(new StageName("Renamed"));

        stage.Status.Should().Be(StageStatus.Ready);
        stage.Name.Value.Should().Be("Renamed");
    }

    private Stage CreateReadyChampionship()
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        return stage;
    }
}
