// -----------------------------------------------------------------------
// <copyright file="20260814100833_InitialCompetition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCompetition : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "competitions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                regulation = table.Column<string>(type: "jsonb", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                completion_mode = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_competitions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "competition_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                team_id = table.Column<Guid>(type: "uuid", nullable: false),
                display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                competition_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_competition_entries", x => x.id);
                table.ForeignKey(
                    name: "FK_competition_entries_competitions_competition_id",
                    column: x => x.competition_id,
                    principalTable: "competitions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "competition_stage_refs",
            columns: table => new
            {
                competition_id = table.Column<Guid>(type: "uuid", nullable: false),
                stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_competition_stage_refs", x => new { x.competition_id, x.stage_id });
                table.ForeignKey(
                    name: "FK_competition_stage_refs_competitions_competition_id",
                    column: x => x.competition_id,
                    principalTable: "competitions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_competition_entries_competition_id_sort_order",
            table: "competition_entries",
            columns: ["competition_id", "sort_order"],
            unique: true);

        // EF cannot model DEFERRABLE UNIQUE; a unique index would be IMMEDIATE and break StageIds reorder UPDATEs.
        migrationBuilder.Sql(
            """
            ALTER TABLE competition_stage_refs
            ADD CONSTRAINT "AK_competition_stage_refs_competition_id_sort_order"
            UNIQUE (competition_id, sort_order)
            DEFERRABLE INITIALLY DEFERRED;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE competition_stage_refs
            DROP CONSTRAINT IF EXISTS "AK_competition_stage_refs_competition_id_sort_order";
            """);

        migrationBuilder.DropTable(name: "competition_entries");
        migrationBuilder.DropTable(name: "competition_stage_refs");
        migrationBuilder.DropTable(name: "competitions");
    }
}
