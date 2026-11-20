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
    public static TheoryData<bool, int?, bool> TruthTable()
    {
        var data = new TheoryData<bool, int?, bool>
        {
            { true, null, false },
            { true, 0, false },
            { true, 2, true },
            { false, null, true },
            { false, 0, true },
            { false, 1, false }
        };
        return data;
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
