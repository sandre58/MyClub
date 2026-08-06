// -----------------------------------------------------------------------
// <copyright file="StageNameTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stage;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stage;

public sealed class StageNameTests
{
    [Fact]
    public void Constructor_trims_and_accepts_valid_name()
    {
        // Arrange & Act
        var name = new StageName("  Groups  ");

        // Assert
        name.Value.Should().Be("Groups");
        name.ToString().Should().Be("Groups");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_name(string value)
    {
        // Arrange & Act
        var act = () => new StageName(value);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.NameInvalid);
    }

    [Fact]
    public void Constructor_rejects_name_exceeding_max_length()
    {
        // Arrange & Act
        var act = () => new StageName(new string('x', StageName.MaxLength + 1));

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.NameInvalid);
    }

    [Fact]
    public void Equality_is_structural()
    {
        // Arrange & Act & Assert
        new StageName("A").Should().Be(new StageName("A"));
        new StageName("A").Should().NotBe(new StageName("B"));
    }
}
