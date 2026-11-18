// -----------------------------------------------------------------------
// <copyright file="GroupEntriesUniqueViolationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class GroupEntriesUniqueViolationTests
{
    [Fact]
    public void IsMatch_is_true_for_group_entries_unique_violation()
    {
        var exception = Postgres(
            PostgresErrorCodes.UniqueViolation,
            tableName: "group_entries",
            constraintName: "PK_group_entries");

        GroupEntriesUniqueViolation.IsMatch(exception).Should().BeTrue();
        GroupEntriesUniqueViolation.IsMatch(new DbUpdateException("Save failed.", exception)).Should().BeTrue();
    }

    [Fact]
    public void IsMatch_is_true_when_only_the_constraint_name_mentions_group_entries()
    {
        var exception = Postgres(
            PostgresErrorCodes.UniqueViolation,
            tableName: null,
            constraintName: "AK_group_entries_group_id_sort_order");

        GroupEntriesUniqueViolation.IsMatch(exception).Should().BeTrue();
    }

    [Fact]
    public void IsMatch_is_false_for_another_table_or_another_sql_state()
    {
        var otherTable = Postgres(
            PostgresErrorCodes.UniqueViolation,
            tableName: "competitions",
            constraintName: "PK_competitions");
        var otherState = Postgres(
            PostgresErrorCodes.ForeignKeyViolation,
            tableName: "group_entries",
            constraintName: "FK_group_entries_groups_group_id");

        GroupEntriesUniqueViolation.IsMatch(otherTable).Should().BeFalse();
        GroupEntriesUniqueViolation.IsMatch(otherState).Should().BeFalse();
    }

    private static PostgresException Postgres(string sqlState, string? tableName, string? constraintName) =>
        new(
            "duplicate key",
            "ERROR",
            "ERROR",
            sqlState,
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: "public",
            tableName: tableName,
            columnName: null,
            dataTypeName: null,
            constraintName: constraintName,
            file: null,
            line: null,
            routine: null);
}
