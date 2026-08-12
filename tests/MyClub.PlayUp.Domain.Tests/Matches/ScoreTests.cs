// -----------------------------------------------------------------------
// <copyright file="ScoreTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

public sealed class ScoreTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 1)]
    [InlineData(0, 5)]
    public void Constructor_accepts_non_negative_goals(int home, int away)
    {
        // Arrange & Act
        var score = new Score(home, away);

        // Assert
        score.HomeGoals.Should().Be(home);
        score.AwayGoals.Should().Be(away);
        score.ToString().Should().Be($"{home}-{away}");
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-2, -3)]
    public void Constructor_rejects_negative_goals(int home, int away)
    {
        // Arrange & Act
        var act = () => new Score(home, away);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void Equality_is_structural()
    {
        // Arrange & Act & Assert
        new Score(2, 1).Should().Be(new Score(2, 1));
        new Score(2, 1).Should().NotBe(new Score(1, 2));
    }
}
