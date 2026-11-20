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
        yield return [true, null, false];
        yield return [true, 0, false];
        yield return [true, 2, true];
        yield return [false, null, true];
        yield return [false, 0, true];
        yield return [false, 1, false];
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
