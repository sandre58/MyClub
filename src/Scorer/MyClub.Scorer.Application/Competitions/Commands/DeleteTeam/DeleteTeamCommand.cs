// -----------------------------------------------------------------------
// <copyright file="DeleteTeamCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Application.Commands;

namespace MyClub.Scorer.Application.Competitions.Commands.DeleteTeam;

public record DeleteTeamCommand(Guid CompetitionId, Guid Id) : DeleteCommand(Id);
