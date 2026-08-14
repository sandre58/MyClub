using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeferStageSortOrderUniques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF cannot model DEFERRABLE UNIQUE; immediate unique indexes break Arrange* UPDATEs
            // when Stage stays Unchanged (same pattern as competition_stage_refs).
            migrationBuilder.DropIndex(
                name: "IX_rounds_stage_id_sort_order",
                table: "rounds");

            migrationBuilder.DropIndex(
                name: "IX_matchdays_stage_id_sort_order",
                table: "matchdays");

            migrationBuilder.DropIndex(
                name: "IX_groups_stage_id_sort_order",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "IX_group_entries_group_id_sort_order",
                table: "group_entries");

            migrationBuilder.CreateIndex(
                name: "IX_rounds_stage_id",
                table: "rounds",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_matchdays_stage_id",
                table: "matchdays",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_stage_id",
                table: "groups",
                column: "stage_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE groups
                ADD CONSTRAINT "AK_groups_stage_id_sort_order"
                UNIQUE (stage_id, sort_order)
                DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE rounds
                ADD CONSTRAINT "AK_rounds_stage_id_sort_order"
                UNIQUE (stage_id, sort_order)
                DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE matchdays
                ADD CONSTRAINT "AK_matchdays_stage_id_sort_order"
                UNIQUE (stage_id, sort_order)
                DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE group_entries
                ADD CONSTRAINT "AK_group_entries_group_id_sort_order"
                UNIQUE (group_id, sort_order)
                DEFERRABLE INITIALLY DEFERRED;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE group_entries
                DROP CONSTRAINT IF EXISTS "AK_group_entries_group_id_sort_order";

                ALTER TABLE matchdays
                DROP CONSTRAINT IF EXISTS "AK_matchdays_stage_id_sort_order";

                ALTER TABLE rounds
                DROP CONSTRAINT IF EXISTS "AK_rounds_stage_id_sort_order";

                ALTER TABLE groups
                DROP CONSTRAINT IF EXISTS "AK_groups_stage_id_sort_order";
                """);

            migrationBuilder.DropIndex(
                name: "IX_rounds_stage_id",
                table: "rounds");

            migrationBuilder.DropIndex(
                name: "IX_matchdays_stage_id",
                table: "matchdays");

            migrationBuilder.DropIndex(
                name: "IX_groups_stage_id",
                table: "groups");

            migrationBuilder.CreateIndex(
                name: "IX_rounds_stage_id_sort_order",
                table: "rounds",
                columns: new[] { "stage_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matchdays_stage_id_sort_order",
                table: "matchdays",
                columns: new[] { "stage_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_groups_stage_id_sort_order",
                table: "groups",
                columns: new[] { "stage_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_entries_group_id_sort_order",
                table: "group_entries",
                columns: new[] { "group_id", "sort_order" },
                unique: true);
        }
    }
}
