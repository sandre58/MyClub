// -----------------------------------------------------------------------
// <copyright file="GetTeamsQueryHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using MyClub.Scorer.Application.Competitions.Queries.GetTeams.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate.Repositories;
using MyClub.Shared.Application.Abstractions.ErrorHandling;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Application.Competitions.Queries.GetTeams;

/// <summary>
/// MediatR handler for <see cref="GetTeamsQuery"/>.
/// Retrieves all teams of a specific competition as <see cref="TeamDto"/>,
/// with centralized error handling and connection resilience (circuit breaker pattern).
/// </summary>
/// <remarks>
/// - Returns a <see cref="Result{T}"/> containing a list of <see cref="TeamDto"/> or error details.
/// - Handles database failures, transient errors, and connection health automatically.
/// - Designed for use in CQRS query pipelines with MediatR behaviors (logging, monitoring, validation).
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="GetTeamsQueryHandler"/> class.
/// </remarks>
/// <param name="competitionRepository">Repository for accessing competitions and their teams.</param>
/// <param name="mapper">AutoMapper instance for mapping domain entities to DTOs.</param>
/// <param name="resilienceService">Service for connection resilience and circuit breaker pattern.</param>
/// <param name="errorHandler">Centralized persistence error handler for retry and error classification.</param>
public class GetTeamsQueryHandler(
    ICompetitionRepository competitionRepository,
    IMapper mapper,
    IConnectionResilienceService resilienceService,
    IPersistenceErrorHandler errorHandler) : IRequestHandler<GetTeamsQuery, Result<IEnumerable<TeamDto>>>
{
    /// <summary>
    /// Handles the <see cref="GetTeamsQuery"/> request.
    /// Retrieves all teams for the specified competition, applies error handling and connection resilience,
    /// and maps the result to <see cref="TeamDto"/>.
    /// </summary>
    /// <param name="query">The query containing the competition identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a list of <see cref="TeamDto"/> if successful,
    /// or error details if the operation fails.
    /// </returns>
    public Task<Result<IEnumerable<TeamDto>>> Handle(GetTeamsQuery query, CancellationToken cancellationToken) => resilienceService.ExecuteWithCircuitBreakerAsync(
            () => errorHandler.ExecuteWithErrorHandlingAsync(
                async () =>
                {
                    var competition = await competitionRepository.GetByIdAsync(query.CompetitionId, cancellationToken).ConfigureAwait(false);
                    if (competition is null)
                        return Result.Fail<IEnumerable<TeamDto>>(Failures.NotFound<IEnumerable<TeamDto>>(query.CompetitionId.ToString()));

                    var dtos = competition.Teams.Select(mapper.Map<TeamDto>).ToList();
                    return Result.Success<IEnumerable<TeamDto>>(dtos);
                },
                operationName: "GetTeamsQuery",
                cancellationToken),
            operationName: "GetTeamsQuery",
            cancellationToken);
}
