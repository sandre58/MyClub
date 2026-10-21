// -----------------------------------------------------------------------
// <copyright file="20260914100000_AddStagePlacesPerGroup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyClub.PlayUp.Infrastructure.Persistence;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PlayUpDbContext))]
[Migration("20260914100000_AddStagePlacesPerGroup")]
public partial class AddStagePlacesPerGroup : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "places_per_group",
            table: "stages",
            type: "integer",
            nullable: true);

        // Legacy bridge: copy Groups pot count (used as per-group capacity) into form fact.
        migrationBuilder.Sql(
            """
            UPDATE stages
            SET places_per_group = (
                (stage_regulation::jsonb -> 'DrawRules' -> 'PotRules' ->> 'NumberOfPots')::int
            )
            WHERE places_per_group IS NULL
              AND stage_regulation::jsonb -> 'DrawRules' -> 'PotRules' ->> 'NumberOfPots' IS NOT NULL
              AND (stage_regulation::jsonb -> 'DrawRules' -> 'PotRules' ->> 'NumberOfPots')::int >= 2;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "places_per_group",
            table: "stages");
    }
}
