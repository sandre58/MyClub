// -----------------------------------------------------------------------
// <copyright file="MatchRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class MatchRulesTests
{
    private static MatchDuration StandardDuration() => new(45, 2, 15);

    private static AdministrativeResultPolicy StandardAdmin() => new(3, 0);

    [Fact]
    public void Absent_optional_policies_mean_disabled()
    {
        // Arrange & Act
        var rules = new MatchRules(StandardDuration(), StandardAdmin());

        // Assert
        rules.ExtraTimePolicy.Should().BeNull();
        rules.PenaltyShootoutPolicy.Should().BeNull();
        rules.AdministrativeResultPolicy.Should().NotBeNull();
    }

    [Fact]
    public void Present_optional_policies_mean_enabled()
    {
        // Arrange
        var extraTime = new ExtraTimePolicy(15, 2);
        var shootout = new PenaltyShootoutPolicy(5);

        // Act
        var rules = new MatchRules(StandardDuration(), StandardAdmin(), extraTime, shootout);

        // Assert
        rules.ExtraTimePolicy.Should().Be(extraTime);
        rules.PenaltyShootoutPolicy.Should().Be(shootout);
    }

    [Fact]
    public void Equality_treats_null_policies_as_equal()
    {
        // Arrange & Act
        var left = new MatchRules(StandardDuration(), StandardAdmin());
        var right = new MatchRules(new MatchDuration(45, 2, 15), new AdministrativeResultPolicy(3, 0));

        // Assert
        left.Should().Be(right);
    }

    [Theory]
    [InlineData(0, 2, 15)]
    [InlineData(45, 0, 15)]
    [InlineData(45, 2, -1)]
    public void MatchDuration_rejects_invalid_values(int duration, int periods, int halfTime)
    {
        // Arrange & Act
        var act = () => new MatchDuration(duration, periods, halfTime);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.MatchDurationInvalid);
    }

    [Fact]
    public void ExtraTimePolicy_rejects_invalid_duration()
    {
        // Arrange & Act
        var act = () => new ExtraTimePolicy(0, 2);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.ExtraTimePolicyInvalid);
    }

    [Fact]
    public void PenaltyShootoutPolicy_rejects_zero_kicks()
    {
        // Arrange & Act
        var act = () => new PenaltyShootoutPolicy(0);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.PenaltyShootoutPolicyInvalid);
    }

    [Fact]
    public void AdministrativeResultPolicy_rejects_negative_goals()
    {
        // Arrange & Act
        var act = () => new AdministrativeResultPolicy(-1, 0);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.AdministrativeResultPolicyInvalid);
    }
}
