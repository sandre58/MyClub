// -----------------------------------------------------------------------
// <copyright file="SlotEntryUniqueViolationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class SlotEntryUniqueViolationTests
{
    [Fact]
    public void IsMatch_is_true_for_slots_entry_deferrable_unique_violation()
    {
        var exception = Postgres(
            PostgresErrorCodes.UniqueViolation,
            tableName: "slots",
            constraintName: "AK_slots_stage_id_entry_id");

        SlotEntryUniqueViolation.IsMatch(exception).Should().BeTrue();
        SlotEntryUniqueViolation.IsMatch(new DbUpdateException("Save failed.", exception)).Should().BeTrue();
    }

    [Fact]
    public void IsMatch_is_false_for_slots_primary_key_and_other_constraints()
    {
        var slotsPrimaryKey = Postgres(
            PostgresErrorCodes.UniqueViolation,
            tableName: "slots",
            constraintName: "PK_slots");
        var groupEntries = Postgres(
            PostgresErrorCodes.UniqueViolation,
            tableName: "group_entries",
            constraintName: "AK_group_entries_group_id_sort_order");
        var otherState = Postgres(
            PostgresErrorCodes.ForeignKeyViolation,
            tableName: "slots",
            constraintName: "AK_slots_stage_id_entry_id");

        SlotEntryUniqueViolation.IsMatch(slotsPrimaryKey).Should().BeFalse();
        SlotEntryUniqueViolation.IsMatch(groupEntries).Should().BeFalse();
        SlotEntryUniqueViolation.IsMatch(otherState).Should().BeFalse();
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
