// -----------------------------------------------------------------------
// <copyright file="20260922180000_AddStageAffectationAuthoring.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyClub.PlayUp.Infrastructure.Persistence;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds Affectation authoring storage and one-shot historical backfill (not a durable Domain rule).
/// Root stages (no inbound Qual/Prog destination) copy Composition → Affectation; inbound stages stay empty.
/// </summary>
[DbContext(typeof(PlayUpDbContext))]
[Migration("20260922180000_AddStageAffectationAuthoring")]
public partial class AddStageAffectationAuthoring : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "stage_affectation_entries",
            columns: table => new
            {
                stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                entry_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stage_affectation_entries", x => new { x.stage_id, x.entry_id });
                table.ForeignKey(
                    name: "FK_stage_affectation_entries_stages_stage_id",
                    column: x => x.stage_id,
                    principalTable: "stages",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Historical backfill only — heuristic without FeedOrigin.
        // A stage is treated as "root" when no other stage's regulation JSON references it as DestinationStageId.
        migrationBuilder.Sql(
            """
            INSERT INTO stage_affectation_entries (stage_id, entry_id)
            SELECT sce.stage_id, sce.entry_id
            FROM stage_composition_entries sce
            WHERE NOT EXISTS (
                SELECT 1
                FROM stages other
                WHERE other.id <> sce.stage_id
                  AND other.stage_regulation::text LIKE '%"DestinationStageId":"' || sce.stage_id::text || '%'
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "stage_affectation_entries");
    }
}
