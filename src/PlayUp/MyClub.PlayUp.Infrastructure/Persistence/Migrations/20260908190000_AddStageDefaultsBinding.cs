// -----------------------------------------------------------------------
// <copyright file="20260908190000_AddStageDefaultsBinding.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyClub.PlayUp.Infrastructure.Persistence;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(PlayUpDbContext))]
[Migration("20260908190000_AddStageDefaultsBinding")]
public partial class AddStageDefaultsBinding : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing stages: all unbound (safe — do not reconstruct provenance by value equality).
        migrationBuilder.AddColumn<string>(
            name: "defaults_binding",
            table: "stages",
            type: "jsonb",
            nullable: false,
            defaultValue: """{"bound":[]}""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "defaults_binding",
            table: "stages");
    }
}
