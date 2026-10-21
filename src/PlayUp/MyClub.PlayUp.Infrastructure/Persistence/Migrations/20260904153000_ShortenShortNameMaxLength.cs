using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyClub.PlayUp.Infrastructure.Persistence;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PlayUpDbContext))]
[Migration("20260904153000_ShortenShortNameMaxLength")]
public partial class ShortenShortNameMaxLength : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE competitions
            SET short_name = LEFT(short_name, 5)
            WHERE short_name IS NOT NULL AND char_length(short_name) > 5;

            UPDATE competition_entries
            SET short_name = LEFT(short_name, 5)
            WHERE short_name IS NOT NULL AND char_length(short_name) > 5;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "short_name",
            table: "competitions",
            type: "character varying(5)",
            maxLength: 5,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "short_name",
            table: "competition_entries",
            type: "character varying(5)",
            maxLength: 5,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "short_name",
            table: "competitions",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(5)",
            oldMaxLength: 5,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "short_name",
            table: "competition_entries",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(5)",
            oldMaxLength: 5,
            oldNullable: true);
    }
}
