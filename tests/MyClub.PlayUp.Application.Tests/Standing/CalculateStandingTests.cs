// -----------------------------------------------------------------------
// <copyright file="CalculateStandingTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Standing;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standing;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Standing;

public sealed class CalculateStandingTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_forwards_penalties_to_calculator()
    {
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(CompetitionId.New(), StageId.New(), home, away, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var rules = new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]);

        var standing = CalculateStanding.Execute(
            [home, away],
            [match],
            rules,
            penalties: [new StandingPenalty(home, 3)]);

        standing.Find(home)!.Points.Should().Be(0);
        standing.Find(away)!.Points.Should().Be(0);
        standing.EntryAt(1).Should().Be(away);
    }

    [Fact]
    public void ToStandingPenalties_maps_entity_fields_without_deduction_logic()
    {
        var stage = Domain.Stage.Stage.Create(
            CompetitionId.New(),
            new Domain.Stage.StageName("L"),
            SampleRegulations.Standard(),
            _clock);
        var entry = EntryId.New();
        stage.AddPenalty(entry, 2, _clock, "note");

        var snapshots = CalculateStanding.ToStandingPenalties(stage.Penalties);

        snapshots.Should().ContainSingle();
        snapshots[0].EntryId.Should().Be(entry);
        snapshots[0].PointsDeducted.Should().Be(2);
    }
}
