using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261030200141_AddResolutionProvenance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "resolution_provenance_kind",
            table: "stage_composition_entries",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "resolution_source_path_id",
            table: "stage_composition_entries",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "resolution_upstream_fingerprint",
            table: "stage_composition_entries",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ResolutionProvenanceKind",
            table: "stage_affectation_entries",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ResolutionSourcePathId",
            table: "stage_affectation_entries",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ResolutionUpstreamFingerprint",
            table: "stage_affectation_entries",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "resolution_provenance_kind",
            table: "slots",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "resolution_source_path_id",
            table: "slots",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "resolution_upstream_fingerprint",
            table: "slots",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "stage_group_resolution_provenances",
            columns: table => new
            {
                group_id = table.Column<Guid>(type: "uuid", nullable: false),
                entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                resolution_provenance_kind = table.Column<int>(type: "integer", nullable: false),
                resolution_source_path_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                resolution_upstream_fingerprint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stage_group_resolution_provenances", x => new { x.stage_id, x.group_id, x.entry_id });
                table.ForeignKey(
                    name: "FK_stage_group_resolution_provenances_stages_stage_id",
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
            name: "stage_group_resolution_provenances");

        migrationBuilder.DropColumn(
            name: "resolution_provenance_kind",
            table: "stage_composition_entries");

        migrationBuilder.DropColumn(
            name: "resolution_source_path_id",
            table: "stage_composition_entries");

        migrationBuilder.DropColumn(
            name: "resolution_upstream_fingerprint",
            table: "stage_composition_entries");

        migrationBuilder.DropColumn(
            name: "ResolutionProvenanceKind",
            table: "stage_affectation_entries");

        migrationBuilder.DropColumn(
            name: "ResolutionSourcePathId",
            table: "stage_affectation_entries");

        migrationBuilder.DropColumn(
            name: "ResolutionUpstreamFingerprint",
            table: "stage_affectation_entries");

        migrationBuilder.DropColumn(
            name: "resolution_provenance_kind",
            table: "slots");

        migrationBuilder.DropColumn(
            name: "resolution_source_path_id",
            table: "slots");

        migrationBuilder.DropColumn(
            name: "resolution_upstream_fingerprint",
            table: "slots");
    }
}
