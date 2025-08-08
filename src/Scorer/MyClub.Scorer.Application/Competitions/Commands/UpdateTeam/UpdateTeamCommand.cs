// -----------------------------------------------------------------------
// <copyright file="UpdateTeamCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Application.Commands;

namespace MyClub.Scorer.Application.Competitions.Commands.UpdateTeam;

/// <summary>
/// Command to update an existing team's information within a competition.
/// This command encapsulates all the data required to modify a team's properties
/// while maintaining referential integrity and business constraints.
/// </summary>
/// <param name="CompetitionId">The unique identifier of the competition containing the team.</param>
/// <param name="Id">The unique identifier of the team to update.</param>
/// <param name="Name">The updated full name of the team.</param>
/// <param name="ShortName">The updated abbreviated name of the team. If not provided, may be auto-generated.</param>
/// <param name="Logo">Updated binary data representing the team's logo/badge. Null preserves existing logo.</param>
/// <param name="StadiumId">Updated identifier of the team's home stadium. Null removes stadium assignment.</param>
public record UpdateTeamCommand(Guid CompetitionId, Guid Id, string Name, string? ShortName = null, byte[]? Logo = null, Guid? StadiumId = null) : UpdateCommand(Id);
