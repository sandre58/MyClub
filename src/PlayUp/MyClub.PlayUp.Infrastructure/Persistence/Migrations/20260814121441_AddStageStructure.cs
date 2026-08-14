using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStageStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    stage_regulation = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stages", x => x.id);
                    table.ForeignKey(
                        name: "FK_stages_competitions_competition_id",
                        column: x => x.competition_id,
                        principalTable: "competitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_groups_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "matchdays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matchdays", x => x.id);
                    table.ForeignKey(
                        name: "FK_matchdays_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tie_format = table.Column<string>(type: "jsonb", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rounds", x => x.id);
                    table.ForeignKey(
                        name: "FK_rounds_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "slots",
                columns: table => new
                {
                    slot_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slots", x => new { x.stage_id, x.slot_key });
                    table.ForeignKey(
                        name: "FK_slots_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stage_direct_assignments",
                columns: table => new
                {
                    slot_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stage_direct_assignments", x => new { x.stage_id, x.slot_key });
                    table.ForeignKey(
                        name: "FK_stage_direct_assignments_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_entries",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_entries", x => new { x.group_id, x.entry_id });
                    table.ForeignKey(
                        name: "FK_group_entries_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fixtures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_a_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    slot_b_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    matchday_id = table.Column<Guid>(type: "uuid", nullable: true),
                    round_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fixtures", x => x.id);
                    table.CheckConstraint("CK_fixtures_round_xor_matchday", "(\"round_id\" IS NOT NULL AND \"matchday_id\" IS NULL) OR (\"round_id\" IS NULL AND \"matchday_id\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_fixtures_matchdays_matchday_id",
                        column: x => x.matchday_id,
                        principalTable: "matchdays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fixtures_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fixture_attachments",
                columns: table => new
                {
                    leg_index = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fixture_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fixture_attachments", x => new { x.fixture_id, x.leg_index });
                    table.ForeignKey(
                        name: "FK_fixture_attachments_fixtures_fixture_id",
                        column: x => x.fixture_id,
                        principalTable: "fixtures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fixture_attachments_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_matches_stage_id",
                table: "matches",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_competition_stage_refs_stage_id",
                table: "competition_stage_refs",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixture_attachments_match_id",
                table: "fixture_attachments",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_matchday_id",
                table: "fixtures",
                column: "matchday_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixtures_round_id",
                table: "fixtures",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_entries_group_id_sort_order",
                table: "group_entries",
                columns: new[] { "group_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_groups_stage_id_sort_order",
                table: "groups",
                columns: new[] { "stage_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matchdays_stage_id_sort_order",
                table: "matchdays",
                columns: new[] { "stage_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rounds_stage_id_sort_order",
                table: "rounds",
                columns: new[] { "stage_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stages_competition_id",
                table: "stages",
                column: "competition_id");

            migrationBuilder.AddForeignKey(
                name: "FK_competition_stage_refs_stages_stage_id",
                table: "competition_stage_refs",
                column: "stage_id",
                principalTable: "stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_matches_stages_stage_id",
                table: "matches",
                column: "stage_id",
                principalTable: "stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_competition_stage_refs_stages_stage_id",
                table: "competition_stage_refs");

            migrationBuilder.DropForeignKey(
                name: "FK_matches_stages_stage_id",
                table: "matches");

            migrationBuilder.DropTable(
                name: "fixture_attachments");

            migrationBuilder.DropTable(
                name: "group_entries");

            migrationBuilder.DropTable(
                name: "slots");

            migrationBuilder.DropTable(
                name: "stage_direct_assignments");

            migrationBuilder.DropTable(
                name: "fixtures");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "matchdays");

            migrationBuilder.DropTable(
                name: "rounds");

            migrationBuilder.DropTable(
                name: "stages");

            migrationBuilder.DropIndex(
                name: "IX_matches_stage_id",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_competition_stage_refs_stage_id",
                table: "competition_stage_refs");
        }
    }
}
