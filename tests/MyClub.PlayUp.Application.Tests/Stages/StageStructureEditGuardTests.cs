// -----------------------------------------------------------------------
// <copyright file="StageStructureEditGuardTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class StageStructureEditGuardTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AddStageMatchday_in_Ready_throws_StructureNotMutable()
    {
        var competition = CreateCompetition.Execute("Guard Ready", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Championship(), _clock);
        stage.Prepare(_clock);

        var act = () => AddStageMatchday.Execute(stage, competition, 2, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
        stage.Status.Should().Be(StageStatus.Ready);
    }
}
