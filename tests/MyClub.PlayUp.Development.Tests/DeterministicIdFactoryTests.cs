// -----------------------------------------------------------------------
// <copyright file="DeterministicIdFactoryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Development.Runtime;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

public sealed class DeterministicIdFactoryTests
{
    [Fact]
    public void Same_inputs_produce_same_ids()
    {
        var a = new DeterministicIdFactory("group-stage-mid", 42);
        var b = new DeterministicIdFactory("group-stage-mid", 42);

        a.Competition().Should().Be(b.Competition());
        a.Stage().Should().Be(b.Stage());
        a.Team("team-0").Should().Be(b.Team("team-0"));
        a.Match("g-0-m-1").Should().Be(b.Match("g-0-m-1"));
    }

    [Fact]
    public void Different_kinds_do_not_collide()
    {
        var ids = new DeterministicIdFactory("s", 1);
        ids.Create("competition", "x").Should().NotBe(ids.Create("stage", "x"));
        ids.Create("team", "x").Should().NotBe(ids.Create("entry", "x"));
    }

    [Fact]
    public void Different_scenario_or_seed_differs()
    {
        var a = new DeterministicIdFactory("a", 1).Competition();
        var b = new DeterministicIdFactory("b", 1).Competition();
        var c = new DeterministicIdFactory("a", 2).Competition();
        a.Should().NotBe(b);
        a.Should().NotBe(c);
    }
}
