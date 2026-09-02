// -----------------------------------------------------------------------
// <copyright file="20260902100000_MatchDeclaredParticipationCompositeKey.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class MatchDeclaredParticipationCompositeKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_match_declared_participations",
            table: "match_declared_participations");

        migrationBuilder.AddPrimaryKey(
            name: "PK_match_declared_participations",
            table: "match_declared_participations",
            columns: ["match_id", "id"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_match_declared_participations",
            table: "match_declared_participations");

        migrationBuilder.AddPrimaryKey(
            name: "PK_match_declared_participations",
            table: "match_declared_participations",
            column: "id");
    }
}
