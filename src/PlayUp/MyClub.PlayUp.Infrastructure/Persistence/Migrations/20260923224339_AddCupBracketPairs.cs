using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCupBracketPairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bracket_pair_key",
                table: "fixtures",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stage_bracket_pairs",
                columns: table => new
                {
                    pair_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_a_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slot_b_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stage_bracket_pairs", x => new { x.stage_id, x.pair_key });
                    table.ForeignKey(
                        name: "FK_stage_bracket_pairs_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Backfill (verrou 4): structural pairs only from slot order — never invent from fixtures.
            // Cup heuristic: has rounds, no matchdays. Natural-sort S{n} keys.
            migrationBuilder.Sql(
                """
                INSERT INTO stage_bracket_pairs (stage_id, pair_key, slot_a_key, slot_b_key)
                SELECT stage_id,
                       'P' || ((rn + 1) / 2)::text,
                       slot_key,
                       lead_key
                FROM (
                    SELECT stage_id,
                           slot_key,
                           rn,
                           lead(slot_key) OVER (PARTITION BY stage_id ORDER BY rn) AS lead_key,
                           cnt
                    FROM (
                        SELECT s.stage_id,
                               s.slot_key,
                               row_number() OVER (
                                   PARTITION BY s.stage_id
                                   ORDER BY
                                       CASE
                                           WHEN s.slot_key ~ '^S[0-9]+$'
                                               THEN CAST(substring(s.slot_key FROM 2) AS integer)
                                           ELSE 2147483647
                                       END,
                                       s.slot_key
                               ) AS rn,
                               count(*) OVER (PARTITION BY s.stage_id) AS cnt
                        FROM slots AS s
                        WHERE EXISTS (SELECT 1 FROM rounds AS r WHERE r.stage_id = s.stage_id)
                          AND NOT EXISTS (SELECT 1 FROM matchdays AS m WHERE m.stage_id = s.stage_id)
                    ) AS numbered
                    WHERE cnt > 0 AND (cnt % 2) = 0
                ) AS paired
                WHERE (rn % 2) = 1 AND lead_key IS NOT NULL;
                """);

            // Attach Fixture.BracketPairKey only when slots match a seeded pair (A/B or B/A).
            // PostgreSQL: do not reference the UPDATE target alias inside FROM/JOIN — join via WHERE.
            migrationBuilder.Sql(
                """
                UPDATE fixtures AS f
                SET bracket_pair_key = bp.pair_key
                FROM stage_bracket_pairs AS bp
                INNER JOIN rounds AS r ON r.stage_id = bp.stage_id
                WHERE f.round_id = r.id
                  AND f.bracket_pair_key IS NULL
                  AND f.slot_a_key IS NOT NULL
                  AND f.slot_b_key IS NOT NULL
                  AND (
                      (f.slot_a_key = bp.slot_a_key AND f.slot_b_key = bp.slot_b_key)
                      OR (f.slot_a_key = bp.slot_b_key AND f.slot_b_key = bp.slot_a_key)
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stage_bracket_pairs");

            migrationBuilder.DropColumn(
                name: "bracket_pair_key",
                table: "fixtures");
        }
    }
}
