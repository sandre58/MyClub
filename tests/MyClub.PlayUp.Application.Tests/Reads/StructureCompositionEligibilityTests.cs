// -----------------------------------------------------------------------
// <copyright file="StructureCompositionEligibilityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureCompositionEligibilityTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void CountRoster_excludes_withdrawn_composition_members()
    {
        var situation = TestCompetition.Create("Roster", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var withdrawn = situation.Competition.Entries[0].Id;
        situation.Competition.Prepare(_clock);
        situation.Competition.Start(_clock);
        WithdrawEntry.Execute(situation.Competition, withdrawn, situation.Stages, [], _clock);

        var (active, ineligible) = StructureCompositionEligibility.CountRoster(
            situation.Competition,
            stage.CompositionEntries);

        stage.CompositionEntries.Should().HaveCount(3);
        active.Should().Be(2);
        ineligible.Should().Be(1);
    }
}
