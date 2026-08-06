// -----------------------------------------------------------------------
// <copyright file="MatchResultTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Match;

public sealed class MatchResultTests
{
    [Fact]
    public void Constructor_accepts_valid_type_and_score()
    {
        // Arrange & Act
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        // Assert
        result.Type.Should().Be(ResultType.Played);
        result.Score.Should().Be(new Score(2, 1));
    }

    [Fact]
    public void Constructor_rejects_undefined_result_type()
    {
        // Arrange & Act
        var act = () => new MatchResult((ResultType)999, new Score(0, 0));

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Theory]
    [InlineData(ResultType.Forfeit)]
    [InlineData(ResultType.WalkOver)]
    [InlineData(ResultType.Administrative)]
    public void Constructor_accepts_all_defined_result_types(ResultType type)
    {
        // Arrange & Act
        var result = new MatchResult(type, new Score(3, 0));

        // Assert
        result.Type.Should().Be(type);
    }

    [Fact]
    public void Equality_is_structural()
    {
        // Arrange
        var left = new MatchResult(ResultType.Played, new Score(2, 1));
        var right = new MatchResult(ResultType.Played, new Score(2, 1));
        var different = new MatchResult(ResultType.Forfeit, new Score(3, 0));

        // Assert
        left.Should().Be(right);
        left.Should().NotBe(different);
    }
}
