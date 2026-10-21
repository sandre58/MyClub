using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceLogoUriWithLogoMediaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "logo_media_id",
                table: "competitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "logo_media_id",
                table: "competition_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "logo_uri",
                table: "competitions");

            migrationBuilder.DropColumn(
                name: "logo_uri",
                table: "competition_entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logo_media_id",
                table: "competitions");

            migrationBuilder.DropColumn(
                name: "logo_media_id",
                table: "competition_entries");

            migrationBuilder.AddColumn<string>(
                name: "logo_uri",
                table: "competitions",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_uri",
                table: "competition_entries",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }
    }
}
