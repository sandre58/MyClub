using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.Scorer.Infrastructure.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Competitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Logo = table.Column<byte>(type: "INTEGER", nullable: true),
                    RegulationTimeNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RegulationTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    RegulationTimeHalfTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ExtraTimeNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    ExtraTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    ExtraTimeHalfTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    NumberOfPenaltyShootouts = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowedCards = table.Column<string>(type: "TEXT", nullable: false),
                    CompetitionType = table.Column<int>(type: "INTEGER", nullable: false),
                    PenaltyPoints = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matchdays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    PostponedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    OriginDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPostponed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matchdays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    AncestorRoundId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsConsolation = table.Column<bool>(type: "INTEGER", nullable: false),
                    Format = table.Column<string>(type: "TEXT", nullable: false),
                    AllowedCards = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rounds_Rounds_AncestorRoundId",
                        column: x => x.AncestorRoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    AncestorStageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsConsolation = table.Column<bool>(type: "INTEGER", nullable: false),
                    RegulationTimeNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RegulationTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    RegulationTimeHalfTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ExtraTimeNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    ExtraTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    ExtraTimeHalfTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    NumberOfPenaltyShootouts = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowedCards = table.Column<string>(type: "TEXT", nullable: false),
                    StageType = table.Column<int>(type: "INTEGER", nullable: false),
                    PenaltyPoints = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stages_Stages_AncestorStageId",
                        column: x => x.AncestorStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeagueStandingLabels",
                columns: table => new
                {
                    LeagueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Start = table.Column<int>(type: "INTEGER", nullable: false),
                    End = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueStandingLabels", x => new { x.LeagueId, x.Id });
                    table.ForeignKey(
                        name: "FK_LeagueStandingLabels_Competitions_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueStandingRuleSets",
                columns: table => new
                {
                    LeagueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Comparer = table.Column<string>(type: "TEXT", nullable: false),
                    PointsByOutcome = table.Column<string>(type: "TEXT", nullable: false),
                    Columns = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueStandingRuleSets", x => x.LeagueId);
                    table.ForeignKey(
                        name: "FK_LeagueStandingRuleSets_Competitions_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stadiums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Ground = table.Column<string>(type: "TEXT", nullable: false),
                    Street = table.Column<string>(type: "TEXT", nullable: true),
                    PostalCode = table.Column<string>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stadiums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stadiums_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueMatchdays",
                columns: table => new
                {
                    LeagueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchdayId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueMatchdays", x => new { x.LeagueId, x.MatchdayId });
                    table.ForeignKey(
                        name: "FK_LeagueMatchdays_Competitions_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueMatchdays_Matchdays_MatchdayId",
                        column: x => x.MatchdayId,
                        principalTable: "Matchdays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CupRounds",
                columns: table => new
                {
                    CupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CupRounds", x => new { x.CupId, x.RoundId });
                    table.ForeignKey(
                        name: "FK_CupRounds_Competitions_CupId",
                        column: x => x.CupId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CupRounds_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fixtures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Team1 = table.Column<string>(type: "TEXT", nullable: false),
                    Team2 = table.Column<string>(type: "TEXT", nullable: false),
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fixtures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fixtures_Rounds_RoundTempId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoundStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    OriginDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPostponed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundStages_Rounds_RoundTempId1",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoundTeam",
                columns: table => new
                {
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Team = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundTeam", x => new { x.RoundId, x.Team });
                    table.ForeignKey(
                        name: "FK_RoundTeam_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipStageMatchdays",
                columns: table => new
                {
                    ChampionshipStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchdayId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipStageMatchdays", x => new { x.ChampionshipStageId, x.MatchdayId });
                    table.ForeignKey(
                        name: "FK_ChampionshipStageMatchdays_Matchdays_MatchdayId",
                        column: x => x.MatchdayId,
                        principalTable: "Matchdays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChampionshipStageMatchdays_Stages_ChampionshipStageId",
                        column: x => x.ChampionshipStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipStageStandingLabels",
                columns: table => new
                {
                    ChampionshipStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Start = table.Column<int>(type: "INTEGER", nullable: false),
                    End = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipStageStandingLabels", x => new { x.ChampionshipStageId, x.Id });
                    table.ForeignKey(
                        name: "FK_ChampionshipStageStandingLabels_Stages_ChampionshipStageId",
                        column: x => x.ChampionshipStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChampionshipStageStandingRuleSets",
                columns: table => new
                {
                    ChampionshipStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Comparer = table.Column<string>(type: "TEXT", nullable: false),
                    PointsByOutcome = table.Column<string>(type: "TEXT", nullable: false),
                    Columns = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChampionshipStageStandingRuleSets", x => x.ChampionshipStageId);
                    table.ForeignKey(
                        name: "FK_ChampionshipStageStandingRuleSets_Stages_ChampionshipStageId",
                        column: x => x.ChampionshipStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    GroupStageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Stages_GroupStageId",
                        column: x => x.GroupStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupStageMatchdays",
                columns: table => new
                {
                    GroupStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchdayId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupStageMatchdays", x => new { x.GroupStageId, x.MatchdayId });
                    table.ForeignKey(
                        name: "FK_GroupStageMatchdays_Matchdays_MatchdayId",
                        column: x => x.MatchdayId,
                        principalTable: "Matchdays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupStageMatchdays_Stages_GroupStageId",
                        column: x => x.GroupStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupStageStandingLabels",
                columns: table => new
                {
                    GroupStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Start = table.Column<int>(type: "INTEGER", nullable: false),
                    End = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupStageStandingLabels", x => new { x.GroupStageId, x.Id });
                    table.ForeignKey(
                        name: "FK_GroupStageStandingLabels_Stages_GroupStageId",
                        column: x => x.GroupStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupStageStandingRuleSets",
                columns: table => new
                {
                    GroupStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Comparer = table.Column<string>(type: "TEXT", nullable: false),
                    PointsByOutcome = table.Column<string>(type: "TEXT", nullable: false),
                    Columns = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupStageStandingRuleSets", x => x.GroupStageId);
                    table.ForeignKey(
                        name: "FK_GroupStageStandingRuleSets_Stages_GroupStageId",
                        column: x => x.GroupStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnockoutStageRounds",
                columns: table => new
                {
                    KnockoutStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoundId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnockoutStageRounds", x => new { x.KnockoutStageId, x.RoundId });
                    table.ForeignKey(
                        name: "FK_KnockoutStageRounds_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnockoutStageRounds_Stages_KnockoutStageId",
                        column: x => x.KnockoutStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageTeam",
                columns: table => new
                {
                    StageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Team = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageTeam", x => new { x.StageId, x.Team });
                    table.ForeignKey(
                        name: "FK_StageTeam_Stages_StageId",
                        column: x => x.StageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentStages",
                columns: table => new
                {
                    TournamentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StageId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentStages", x => new { x.TournamentId, x.StageId });
                    table.ForeignKey(
                        name: "FK_TournamentStages_Competitions_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentStages_Stages_StageId",
                        column: x => x.StageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RegulationTimeNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RegulationTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    RegulationTimeHalfTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ExtraTimeNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    ExtraTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    ExtraTimeHalfTimeDuration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    NumberOfPenaltyShootouts = table.Column<int>(type: "INTEGER", nullable: true),
                    AllowedCards = table.Column<string>(type: "TEXT", nullable: false),
                    OriginDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PostponedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    HomeTeam = table.Column<string>(type: "TEXT", nullable: false),
                    HomeIsWithdrawn = table.Column<bool>(type: "INTEGER", nullable: false),
                    AwayTeam = table.Column<string>(type: "TEXT", nullable: false),
                    AwayIsWithdrawn = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNeutralStadium = table.Column<bool>(type: "INTEGER", nullable: false),
                    StadiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AfterExtraTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Logo = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    StadiumId = table.Column<Guid>(type: "TEXT", nullable: true),
                    HomeColor = table.Column<string>(type: "TEXT", nullable: true),
                    AwayColor = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Teams_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GroupTeam",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Team = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupTeam", x => new { x.GroupId, x.Team });
                    table.ForeignKey(
                        name: "FK_GroupTeam_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AwayCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    Infraction = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Minute = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwayCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwayCards_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AwayGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ScorerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssistId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Minute = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwayGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwayGoals_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AwayPenaltyShootouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TakerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwayPenaltyShootouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwayPenaltyShootouts_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomeCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    Infraction = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    PlayerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Minute = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeCards_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomeGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ScorerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssistId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Minute = table.Column<int>(type: "INTEGER", nullable: true),
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeGoals_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomePenaltyShootouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TakerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomePenaltyShootouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomePenaltyShootouts_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchdayMatches",
                columns: table => new
                {
                    MatchdayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchdayMatches", x => new { x.MatchdayId, x.MatchId });
                    table.ForeignKey(
                        name: "FK_MatchdayMatches_Matchdays_MatchdayId",
                        column: x => x.MatchdayId,
                        principalTable: "Matchdays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchdayMatches_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoundStageMatches",
                columns: table => new
                {
                    RoundStageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundStageMatches", x => new { x.RoundStageId, x.MatchId });
                    table.ForeignKey(
                        name: "FK_RoundStageMatches_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoundStageMatches_RoundStages_RoundStageId",
                        column: x => x.RoundStageId,
                        principalTable: "RoundStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Managers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Photo = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", nullable: false),
                    LicenseNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Managers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Managers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Photo = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", nullable: false),
                    LicenseNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AwayCards_MatchId",
                table: "AwayCards",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AwayGoals_MatchId",
                table: "AwayGoals",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AwayPenaltyShootouts_MatchId",
                table: "AwayPenaltyShootouts",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipStageMatchdays_ChampionshipStageId",
                table: "ChampionshipStageMatchdays",
                column: "ChampionshipStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipStageMatchdays_MatchdayId",
                table: "ChampionshipStageMatchdays",
                column: "MatchdayId");

            migrationBuilder.CreateIndex(
                name: "IX_CupRounds_CupId",
                table: "CupRounds",
                column: "CupId");

            migrationBuilder.CreateIndex(
                name: "IX_CupRounds_RoundId",
                table: "CupRounds",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Fixtures_RoundId",
                table: "Fixtures",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_GroupStageId",
                table: "Groups",
                column: "GroupStageId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupStageMatchdays_GroupStageId",
                table: "GroupStageMatchdays",
                column: "GroupStageId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupStageMatchdays_MatchdayId",
                table: "GroupStageMatchdays",
                column: "MatchdayId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupTeam_GroupId",
                table: "GroupTeam",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeCards_MatchId",
                table: "HomeCards",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeGoals_MatchId",
                table: "HomeGoals",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_HomePenaltyShootouts_MatchId",
                table: "HomePenaltyShootouts",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_KnockoutStageRounds_KnockoutStageId",
                table: "KnockoutStageRounds",
                column: "KnockoutStageId");

            migrationBuilder.CreateIndex(
                name: "IX_KnockoutStageRounds_RoundId",
                table: "KnockoutStageRounds",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMatchdays_LeagueId",
                table: "LeagueMatchdays",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMatchdays_MatchdayId",
                table: "LeagueMatchdays",
                column: "MatchdayId");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_TeamId",
                table: "Managers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchdayMatches_MatchdayId",
                table: "MatchdayMatches",
                column: "MatchdayId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchdayMatches_MatchId",
                table: "MatchdayMatches",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_StadiumId",
                table: "Matches",
                column: "StadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_AncestorRoundId",
                table: "Rounds",
                column: "AncestorRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundStageMatches_MatchId",
                table: "RoundStageMatches",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundStageMatches_RoundStageId",
                table: "RoundStageMatches",
                column: "RoundStageId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundStages_RoundId",
                table: "RoundStages",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundTeam_RoundId",
                table: "RoundTeam",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Stadiums_CompetitionId",
                table: "Stadiums",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Stages_AncestorStageId",
                table: "Stages",
                column: "AncestorStageId");

            migrationBuilder.CreateIndex(
                name: "IX_StageTeam_StageId",
                table: "StageTeam",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CompetitionId",
                table: "Teams",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_StadiumId",
                table: "Teams",
                column: "StadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStages_StageId",
                table: "TournamentStages",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStages_TournamentId",
                table: "TournamentStages",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AwayCards");

            migrationBuilder.DropTable(
                name: "AwayGoals");

            migrationBuilder.DropTable(
                name: "AwayPenaltyShootouts");

            migrationBuilder.DropTable(
                name: "ChampionshipStageMatchdays");

            migrationBuilder.DropTable(
                name: "ChampionshipStageStandingLabels");

            migrationBuilder.DropTable(
                name: "ChampionshipStageStandingRuleSets");

            migrationBuilder.DropTable(
                name: "CupRounds");

            migrationBuilder.DropTable(
                name: "Fixtures");

            migrationBuilder.DropTable(
                name: "GroupStageMatchdays");

            migrationBuilder.DropTable(
                name: "GroupStageStandingLabels");

            migrationBuilder.DropTable(
                name: "GroupStageStandingRuleSets");

            migrationBuilder.DropTable(
                name: "GroupTeam");

            migrationBuilder.DropTable(
                name: "HomeCards");

            migrationBuilder.DropTable(
                name: "HomeGoals");

            migrationBuilder.DropTable(
                name: "HomePenaltyShootouts");

            migrationBuilder.DropTable(
                name: "KnockoutStageRounds");

            migrationBuilder.DropTable(
                name: "LeagueMatchdays");

            migrationBuilder.DropTable(
                name: "LeagueStandingLabels");

            migrationBuilder.DropTable(
                name: "LeagueStandingRuleSets");

            migrationBuilder.DropTable(
                name: "Managers");

            migrationBuilder.DropTable(
                name: "MatchdayMatches");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "RoundStageMatches");

            migrationBuilder.DropTable(
                name: "RoundTeam");

            migrationBuilder.DropTable(
                name: "StageTeam");

            migrationBuilder.DropTable(
                name: "TournamentStages");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Matchdays");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "RoundStages");

            migrationBuilder.DropTable(
                name: "Stages");

            migrationBuilder.DropTable(
                name: "Stadiums");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropTable(
                name: "Competitions");
        }
    }
}
