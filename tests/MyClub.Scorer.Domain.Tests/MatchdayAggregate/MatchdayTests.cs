// -----------------------------------------------------------------------
// <copyright file="MatchdayTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using AutoFixture;
using FluentAssertions;
using MyClub.Scorer.Domain.MatchdayAggregate;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.MatchdayAggregate;

public class MatchdayTests
{
    [Fact]
    public void Create_ShouldInitializeMatchdayWithCorrectValues()
    {
        var date = new Fixture().Create<DateTime>();
        var name = new Fixture().Create<string>();
        var shortName = new Fixture().Create<string>();

        var matchday = Matchday.Create(date, name, shortName);

        matchday.OriginDate.Should().Be(date);
        matchday.DisplayName.Name.Should().Be(name);
        matchday.DisplayName.ShortName.Should().Be(shortName);
    }

    [Fact]
    public void Create_ShouldSetShortNameToInitialsIfNull()
    {
        var date = new Fixture().Create<DateTime>();
        var name = new Fixture().Create<string>();

        var matchday = Matchday.Create(date, name);

        matchday.DisplayName.Name.Should().Be(name);
        matchday.DisplayName.ShortName.Should().NotBeNullOrEmpty();
        matchday.DisplayName.ShortName.Should().NotBe(name); // Should be initials
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        var date = new Fixture().Create<DateTime>();
        var name = new Fixture().Create<string>();
        var shortName = new Fixture().Create<string>();

        var matchday = Matchday.Create(date, name, shortName);

        matchday.ToString().Should().Be(matchday.DisplayName.ToString());
    }
}
