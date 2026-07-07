// -----------------------------------------------------------------------
// <copyright file="GetTeamsQuery.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Application.Competitions.Queries.GetTeams.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Shared.Application.Queries;

namespace MyClub.Scorer.Application.Competitions.Queries.GetTeams;

/// <summary>
/// Query to retrieve all teams as DTOs for a specific competition.
/// </summary>
public sealed record GetTeamsQuery(CompetitionId CompetitionId) : GetAllQuery<TeamDto>;
