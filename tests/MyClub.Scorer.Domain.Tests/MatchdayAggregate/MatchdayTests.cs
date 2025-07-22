// -----------------------------------------------------------------------
// <copyright file="MatchdayTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FluentAssertions;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Tests.Common;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.MatchdayAggregate;

public class MatchdayTests : TestBase
{
    [Fact]
    public void Create_ShouldInitializeMatchdayWithCorrectValues()
    {
        var date = Create<DateTime>();
        var name = Create<string>();
        var shortName = Create<string>();

        var matchday = Matchday.Create(date, name, shortName);

        matchday.OriginDate.Should().Be(date);
        matchday.DisplayName.Name.Should().Be(name);
        matchday.DisplayName.ShortName.Should().Be(shortName);
    }

    [Fact]
    public void Create_ShouldSetShortNameToInitialsIfNull()
    {
        var date = Create<DateTime>();
        var name = Create<string>();

        var matchday = Matchday.Create(date, name);

        matchday.DisplayName.Name.Should().Be(name);
        matchday.DisplayName.ShortName.Should().NotBeNullOrEmpty();
        matchday.DisplayName.ShortName.Should().NotBe(name); // Should be initials
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        var date = Create<DateTime>();
        var name = Create<string>();
        var shortName = Create<string>();

        var matchday = Matchday.Create(date, name, shortName);

        matchday.ToString().Should().Be(matchday.DisplayName.ToString());
    }
}
