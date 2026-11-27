#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class BackfillMaterializedRoundTieFormat : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var defaultOneLegJson = TieFormatJson.Serialize(TieFormat.DefaultOneLeg);
        migrationBuilder.Sql(
            $"""
            UPDATE rounds AS r
            SET tie_format = '{defaultOneLegJson}'
            WHERE r.tie_format IS NULL
              AND r.id IN (
                SELECT DISTINCT fr.round_id
                FROM fixtures AS fr
                INNER JOIN fixture_attachments AS fa ON fa.fixture_id = fr.id
                WHERE fr.round_id IS NOT NULL);
            """);
    }

    /// <summary>
    /// Non-reversible: inherited null round formats cannot be distinguished from explicit overrides after stamp.
    /// </summary>
    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}