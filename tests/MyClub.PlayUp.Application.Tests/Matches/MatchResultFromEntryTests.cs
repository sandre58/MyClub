// -----------------------------------------------------------------------
// <copyright file="MatchResultFromEntryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Matches;

public sealed class MatchResultFromEntryTests
{
    [Fact]
    public void ValidateShape_rejects_unknown_result_type_before_match_load()
    {
        var entry = new MatchResultEntry(
            Type: (ResultType)99,
            HomeGoals: 1,
            AwayGoals: 0,
            ExtraTimePlayed: false,
            PenaltyShootoutHomeGoals: null,
            PenaltyShootoutAwayGoals: null);

        var act = () => MatchResultFromEntry.ValidateShape(entry);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }
}
