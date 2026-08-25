// -----------------------------------------------------------------------
// <copyright file="SeedSpecTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Development.Runtime;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

public sealed class SeedSpecTests
{
    [Fact]
    public void Parse_id_only_leaves_progress_unspecified()
    {
        var spec = SeedSpec.Parse("groups");
        spec.Id.Should().Be("groups");
        spec.Progress.Should().BeNull();
        spec.EffectiveProgress.Should().Be(SeedProgress.Running);
    }

    [Theory]
    [InlineData("groups:prepared", "groups", SeedProgress.Prepared)]
    [InlineData("cup:finished", "cup", SeedProgress.Finished)]
    [InlineData("group-stage-mid", "groups", SeedProgress.Running)]
    [InlineData("knockout-qf", "cup", SeedProgress.Running)]
    [InlineData("finished", "groups", SeedProgress.Finished)]
    public void Parse_resolves_progress_and_aliases(string raw, string id, SeedProgress progress)
    {
        var spec = SeedSpec.Parse(raw);
        spec.Id.Should().Be(id);
        spec.Progress.Should().Be(progress);
    }

    [Fact]
    public void Parse_rejects_unknown_progress()
    {
        var act = () => SeedSpec.Parse("groups:nope");
        act.Should().Throw<InvalidOperationException>().WithMessage("*progress*");
    }
}
