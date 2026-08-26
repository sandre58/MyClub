using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionPresentationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "logo_uri",
                table: "competitions",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scheduled_end",
                table: "competitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scheduled_start",
                table: "competitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_name",
                table: "competitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_uri",
                table: "competition_entries",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_color",
                table: "competition_entries",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_color",
                table: "competition_entries",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_name",
                table: "competition_entries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logo_uri",
                table: "competitions");

            migrationBuilder.DropColumn(
                name: "scheduled_end",
                table: "competitions");

            migrationBuilder.DropColumn(
                name: "scheduled_start",
                table: "competitions");

            migrationBuilder.DropColumn(
                name: "short_name",
                table: "competitions");

            migrationBuilder.DropColumn(
                name: "logo_uri",
                table: "competition_entries");

            migrationBuilder.DropColumn(
                name: "primary_color",
                table: "competition_entries");

            migrationBuilder.DropColumn(
                name: "secondary_color",
                table: "competition_entries");

            migrationBuilder.DropColumn(
                name: "short_name",
                table: "competition_entries");
        }
    }
}
