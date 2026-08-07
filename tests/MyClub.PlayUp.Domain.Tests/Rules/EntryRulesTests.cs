// -----------------------------------------------------------------------
// <copyright file="EntryRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class EntryRulesTests
{
    [Fact]
    public void Constructor_accepts_valid_bounds()
    {
        // Arrange & Act
        var rules = new EntryRules(2, 16);

        // Assert
        rules.MinimumTeams.Should().Be(2);
        rules.MaximumTeams.Should().Be(16);
    }

    [Theory]
    [InlineData(0, 16)]
    [InlineData(8, 4)]
    public void Constructor_rejects_invalid_bounds(int min, int max)
    {
        // Arrange & Act
        var act = () => new EntryRules(min, max);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.EntryRulesInvalid);
    }
}
