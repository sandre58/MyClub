// -----------------------------------------------------------------------
// <copyright file="CompetitionLockFailureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class CompetitionLockFailureTests
{
    [Theory]
    [InlineData("55P03")]
    [InlineData("40P01")]
    public void IsCompetitionLockContention_recognizes_lock_sql_states(string sqlState)
    {
        var exception = new PostgresException("message", "severity", "severity", sqlState);
        CompetitionLockFailure.IsCompetitionLockContention(exception).Should().BeTrue();
    }

    [Fact]
    public void IsQueryCanceled_recognizes_57014_without_mapping_it_to_lock_contention()
    {
        var exception = new PostgresException("message", "severity", "severity", "57014");
        CompetitionLockFailure.IsQueryCanceled(exception).Should().BeTrue();
        CompetitionLockFailure.IsCompetitionLockContention(exception).Should().BeFalse();
    }
}
