using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchRecordedDisciplinaryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_recorded_disciplinary_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_recorded_disciplinary_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_recorded_disciplinary_events_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_recorded_disciplinary_events_match_id_sort_order",
                table: "match_recorded_disciplinary_events",
                columns: new[] { "match_id", "sort_order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_recorded_disciplinary_events");
        }
    }
}
