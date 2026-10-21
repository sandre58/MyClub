// -----------------------------------------------------------------------
// <copyright file="StructurePlaceOccupancyCountParityTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Parity with SPA <c>structurePlaceOccupancy.test.ts</c> (O3-A).
/// </summary>
public sealed class StructurePlaceOccupancyCountParityTests
{
    private static readonly Guid EntryE1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EntryE3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid GroupG1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Cup_counts_entry_round_occupants_only()
    {
        var schematic = new StageSchematicDto(
            Guid.Empty,
            Guid.Empty,
            "Cup",
            StageStatus.Draft,
            StructureFormatKind.Cup,
            [
                CupCase("S1", 0, EntryE1),
                CupCase("S2", 0),
                CupCase("S3", 1, EntryE3)
            ],
            []);

        StructurePlaceOccupancyIssues.CountOccupiedPlaces(schematic, StructureFormatKind.Cup)
            .Should()
            .Be(1);
    }

    [Fact]
    public void Groups_counts_qual_feed_as_occupied_without_resolved_team()
    {
        var schematic = new StageSchematicDto(
            Guid.Empty,
            Guid.Empty,
            "Groups",
            StageStatus.Draft,
            StructureFormatKind.Groups,
            [
                new SchematicCaseDto(
                    new SchematicFormPositionDto(
                        StageSchematicAssembler.FormKindGroupPlace,
                        GroupId: GroupG1,
                        Index: 1),
                    new SchematicFeedOriginDto(
                        FeedKind.Qualification,
                        SelectionMode: SelectionMode.Position,
                        SelectionValue: 1),
                    null,
                    null),
                new SchematicCaseDto(
                    new SchematicFormPositionDto(
                        StageSchematicAssembler.FormKindGroupPlace,
                        GroupId: GroupG1,
                        Index: 2),
                    null,
                    null,
                    null)
            ],
            []);

        StructurePlaceOccupancyIssues.CountOccupiedPlaces(schematic, StructureFormatKind.Groups)
            .Should()
            .Be(1);
    }

    [Fact]
    public void Championship_counts_roster_entry_or_feed_as_structural_occupancy()
    {
        var schematic = new StageSchematicDto(
            Guid.Empty,
            Guid.Empty,
            "Championship",
            StageStatus.Draft,
            StructureFormatKind.Championship,
            [
                new SchematicCaseDto(
                    new SchematicFormPositionDto(StageSchematicAssembler.FormKindRosterPlace, Index: 1),
                    null,
                    new SchematicEntryRefDto(EntryE1, "A"),
                    null),
                new SchematicCaseDto(
                    new SchematicFormPositionDto(StageSchematicAssembler.FormKindRosterPlace, Index: 2),
                    new SchematicFeedOriginDto(
                        FeedKind.Qualification,
                        SelectionMode: SelectionMode.Position,
                        SelectionValue: 1),
                    null,
                    null),
                new SchematicCaseDto(
                    new SchematicFormPositionDto(StageSchematicAssembler.FormKindRosterPlace, Index: 3),
                    null,
                    null,
                    null)
            ],
            []);

        StructurePlaceOccupancyIssues.CountOccupiedPlaces(schematic, StructureFormatKind.Championship)
            .Should()
            .Be(2);
    }

    private static SchematicCaseDto CupCase(string slotKey, int roundOrder, Guid? entryId = null) =>
        new(
            new SchematicFormPositionDto(
                StageSchematicAssembler.FormKindCupSlot,
                SlotKey: slotKey,
                RoundOrder: roundOrder),
            null,
            entryId is { } id ? new SchematicEntryRefDto(id, "Team") : null,
            null);
}
