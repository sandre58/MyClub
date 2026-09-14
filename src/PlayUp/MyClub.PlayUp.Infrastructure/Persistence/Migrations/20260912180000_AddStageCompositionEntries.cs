// -----------------------------------------------------------------------
// <copyright file="20260912180000_AddStageCompositionEntries.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyClub.PlayUp.Infrastructure.Persistence;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PlayUpDbContext))]
[Migration("20260912180000_AddStageCompositionEntries")]
public partial class AddStageCompositionEntries : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "stage_composition_entries",
            columns: table => new
            {
                stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                entry_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stage_composition_entries", x => new { x.stage_id, x.entry_id });
                table.ForeignKey(
                    name: "FK_stage_composition_entries_stages_stage_id",
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
            name: "stage_composition_entries");
    }
}
