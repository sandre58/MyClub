// -----------------------------------------------------------------------
// <copyright file="PenaltyShootoutScoreTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Match;

public sealed class PenaltyShootoutScoreTests
{
    [Fact]
    public void Constructor_accepts_non_negative_goals()
    {
        var score = new PenaltyShootoutScore(4, 3);

        score.HomeGoals.Should().Be(4);
        score.AwayGoals.Should().Be(3);
        score.ToString().Should().Be("4-3");
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Constructor_rejects_negative_goals(int home, int away)
    {
        var act = () => new PenaltyShootoutScore(home, away);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }
}
