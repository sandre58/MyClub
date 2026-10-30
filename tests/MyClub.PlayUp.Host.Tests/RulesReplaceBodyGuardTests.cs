// -----------------------------------------------------------------------
// <copyright file="RulesReplaceBodyGuardTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Host.Endpoints;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

public sealed class RulesReplaceBodyGuardTests
{
    public static IEnumerable<object?[]> TruthTable()
    {
        yield return new object?[] { true, null, false };
        yield return new object?[] { true, 0, false };
        yield return new object?[] { true, 2, true };
        yield return new object?[] { false, null, true };
        yield return new object?[] { false, 0, true };
        yield return new object?[] { false, 1, false };
    }

    [Theory]
    [MemberData(nameof(TruthTable))]
    public void EnsureListOrClear_follows_the_list_or_clear_table(bool clear, int? count, bool rejected)
    {
        var act = () => RulesReplaceBodyGuard.EnsureListOrClear(clear, count);

        if (rejected)
        {
            act.Should().Throw<ApplicationFailureException>()
                .Which.Code.Should().Be(ApplicationErrorCodes.RulesReplaceRequiresListOrClear);
        }
        else
        {
            act.Should().NotThrow();
        }
    }
}
