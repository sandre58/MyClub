// -----------------------------------------------------------------------
// <copyright file="UpdateTeamCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Application.Commands;

namespace MyClub.Scorer.Application.Competitions.Commands.UpdateTeam;

public record UpdateTeamCommand(Guid CompetitionId, Guid Id, string Name, string? ShortName = null, byte[]? Logo = null, Guid? StadiumId = null) : UpdateCommand(Id);
