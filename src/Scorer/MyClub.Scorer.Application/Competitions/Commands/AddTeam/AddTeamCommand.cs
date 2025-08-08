// -----------------------------------------------------------------------
// <copyright file="AddTeamCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Application.Commands;

namespace MyClub.Scorer.Application.Competitions.Commands.AddTeam;

/// <summary>
/// Command to add a new team to an existing competition.
/// This command encapsulates all the information required to create and associate
/// a new team with a specific competition while maintaining data integrity.
/// </summary>
/// <param name="CompetitionId">The unique identifier of the competition to add the team to.</param>
/// <param name="Name">The full name of the team (e.g., "Manchester United Football Club").</param>
/// <param name="ShortName">The abbreviated name of the team (e.g., "MUN"). If not provided, will be auto-generated.</param>
/// <param name="Logo">Optional binary data representing the team's logo/badge.</param>
/// <param name="StadiumId">Optional identifier of the team's home stadium. Must be a stadium registered in the competition.</param>
public record AddTeamCommand(Guid CompetitionId, string Name, string? ShortName = null, byte[]? Logo = null, Guid? StadiumId = null) : CreateCommand;
