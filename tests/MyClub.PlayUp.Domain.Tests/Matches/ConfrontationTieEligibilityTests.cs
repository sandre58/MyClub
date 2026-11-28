// -----------------------------------------------------------------------
// <copyright file="ConfrontationTieEligibilityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

public sealed class ConfrontationTieEligibilityTests
{
    private readonly EntryId _entryA = EntryId.New();
    private readonly EntryId _entryB = EntryId.New();

    [Fact]
    public void CreateProof_issues_token_when_aggregate_still_level()
    {
        var tieFormat = TwoLegFormat(awayGoals: false);
        var legs = LevelLegs(new Score(1, 0), new Score(1, 0));

        var proof = ConfrontationTieEligibility.CreateProof(tieFormat, _entryA, _entryB, legs);

        proof.Should().NotBeNull();
        MatchResult.CreateConfrontationTiePenaltyShootout(
            new Score(1, 0),
            false,
            new PenaltyShootoutScore(5, 4),
            proof).ConfrontationTiePenaltyShootout.Should().BeTrue();
    }

    [Fact]
    public void CreateProof_rejects_when_aggregate_not_level()
    {
        var tieFormat = TwoLegFormat(awayGoals: false);
        var legs = LevelLegs(new Score(2, 0), new Score(1, 0));

        var act = () => ConfrontationTieEligibility.CreateProof(tieFormat, _entryA, _entryB, legs);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    private static TieFormat TwoLegFormat(bool awayGoals) =>
        new(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: awayGoals ? new AwayGoalsRule() : null);

    private FixtureLegSnapshot[] LevelLegs(Score leg1, Score leg2) =>
    [
        new(1, MatchId.New(), _entryA, _entryB, MatchStatus.Finished, leg1, false, null),
        new(2, MatchId.New(), _entryB, _entryA, MatchStatus.Finished, leg2, false, new PenaltyShootoutScore(5, 4)),
    ];
}
