// -----------------------------------------------------------------------
// <copyright file="CompetitionNameTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Competitions;

public sealed class CompetitionNameTests
{
    [Fact]
    public void Constructor_trims_and_accepts_valid_name()
    {
        // Arrange & Act
        var name = new CompetitionName("  Cup  ");

        // Assert
        name.Value.Should().Be("Cup");
        name.ToString().Should().Be("Cup");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_name(string value)
    {
        // Arrange & Act
        var act = () => new CompetitionName(value);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.NameInvalid);
    }

    [Fact]
    public void Constructor_rejects_name_longer_than_max()
    {
        // Arrange
        var value = new string('a', CompetitionName.MaxLength + 1);

        // Act
        var act = () => new CompetitionName(value);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.NameInvalid);
    }

    [Fact]
    public void Equality_is_structural_on_normalized_value()
    {
        // Arrange & Act
        var left = new CompetitionName("Cup");
        var right = new CompetitionName("  Cup  ");

        // Assert
        left.Should().Be(right);
    }
}
