// -----------------------------------------------------------------------
// <copyright file="DeleteTeamCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Application.Commands;

namespace MyClub.Scorer.Application.Competitions.Commands.DeleteTeam;

/// <summary>
/// Command to remove a team from a competition.
/// This command handles the safe removal of teams while ensuring data integrity
/// and proper handling of related entities and constraints.
/// </summary>
/// <param name="CompetitionId">The unique identifier of the competition containing the team to remove.</param>
/// <param name="Id">The unique identifier of the team to delete from the competition.</param>
public record DeleteTeamCommand(Guid CompetitionId, Guid Id) : DeleteCommand(Id);
