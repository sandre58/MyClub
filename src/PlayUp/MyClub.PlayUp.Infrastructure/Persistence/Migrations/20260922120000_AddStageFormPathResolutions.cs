// -----------------------------------------------------------------------
// <copyright file="20260922120000_AddStageFormPathResolutions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyClub.PlayUp.Infrastructure.Persistence;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PlayUpDbContext))]
[Migration("20260922120000_AddStageFormPathResolutions")]
public partial class AddStageFormPathResolutions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "stage_form_path_resolutions",
            columns: table => new
            {
                stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                path_fingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                entry_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stage_form_path_resolutions", x => new { x.stage_id, x.path_fingerprint });
                table.ForeignKey(
                    name: "FK_stage_form_path_resolutions_stages_stage_id",
                    column: x => x.stage_id,
                    principalTable: "stages",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "stage_form_path_resolutions");
    }
}
