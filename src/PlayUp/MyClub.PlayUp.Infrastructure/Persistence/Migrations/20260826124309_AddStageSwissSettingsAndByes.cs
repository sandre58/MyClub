using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStageSwissSettingsAndByes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "swiss_round_count",
                table: "stages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stage_swiss_byes",
                columns: table => new
                {
                    round_index = table.Column<int>(type: "integer", nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stage_swiss_byes", x => new { x.stage_id, x.round_index });
                    table.ForeignKey(
                        name: "FK_stage_swiss_byes_stages_stage_id",
                        column: x => x.stage_id,
                        principalTable: "stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stage_swiss_byes");

            migrationBuilder.DropColumn(
                name: "swiss_round_count",
                table: "stages");
        }
    }
}
