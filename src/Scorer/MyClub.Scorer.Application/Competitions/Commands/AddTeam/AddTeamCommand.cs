// -----------------------------------------------------------------------
// <copyright file="AddTeamCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Application.Commands;

namespace MyClub.Scorer.Application.Competitions.Commands.AddTeam;

public record AddTeamCommand(Guid CompetitionId, string Name, string? ShortName = null, byte[]? Logo = null, Guid? StadiumId = null) : CreateCommand;
