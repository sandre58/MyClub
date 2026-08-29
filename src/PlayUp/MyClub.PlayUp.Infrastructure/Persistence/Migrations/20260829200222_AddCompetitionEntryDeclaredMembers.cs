using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionEntryDeclaredMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competition_entry_declared_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competition_entry_declared_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_competition_entry_declared_members_competition_entries_entr~",
                        column: x => x.entry_id,
                        principalTable: "competition_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_competition_entry_declared_members_entry_id_sort_order",
                table: "competition_entry_declared_members",
                columns: new[] { "entry_id", "sort_order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "competition_entry_declared_members");
        }
    }
}
