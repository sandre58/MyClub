// -----------------------------------------------------------------------
// <copyright file="MatchResultImmutabilityArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using MyClub.PlayUp.Domain.Matches;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

public sealed class MatchResultImmutabilityArchitectureTests
{
    private static readonly string[] ImmunePropertyNames =
    [
        nameof(MatchResult.Type),
        nameof(MatchResult.Score),
        nameof(MatchResult.ExtraTimePlayed),
        nameof(MatchResult.PenaltyShootoutScore),
        nameof(MatchResult.ConfrontationTiePenaltyShootout),
    ];

    [Fact]
    public void MatchResult_exposes_no_public_setters_or_init_on_payload_properties()
    {
        var type = typeof(MatchResult);
        foreach (var propertyName in ImmunePropertyNames)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            property.Should().NotBeNull(propertyName);
            property!.SetMethod.Should().BeNull($"{propertyName} must not be publicly settable");
        }
    }
}
