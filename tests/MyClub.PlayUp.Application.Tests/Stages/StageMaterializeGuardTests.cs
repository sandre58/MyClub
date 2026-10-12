// -----------------------------------------------------------------------
// <copyright file="StageMaterializeGuardTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class StageMaterializeGuardTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Materialize_rejects_draft_stage()
    {
        var situation = TestCompetition.Create("M", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();

        var act = () => MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }
}
