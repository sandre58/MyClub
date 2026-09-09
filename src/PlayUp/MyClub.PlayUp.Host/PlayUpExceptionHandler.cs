// -----------------------------------------------------------------------
// <copyright file="PlayUpExceptionHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Host;

/// <summary>
/// Maps Domain / Application failures to minimal ProblemDetails responses.
/// </summary>
/// <param name="logger">Logger for server failures (≥ 500).</param>
internal sealed partial class PlayUpExceptionHandler(ILogger<PlayUpExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, code) = Map(exception);
        if (statusCode is null)
        {
            return false;
        }

        var correlationId = httpContext.GetCorrelationId();
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions = { ["correlationId"] = correlationId }
        };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        if (exception is ApplicationFailureException { Reasons.Count: > 0 } application)
        {
            problem.Extensions["reasons"] = application.Reasons;
        }

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogServerFailure(logger, exception, code, statusCode.Value, httpContext.Request.Path.Value);
        }

        httpContext.Response.StatusCode = statusCode.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Server failure {ErrorCode} returned {StatusCode} for {RequestPath}")]
    private static partial void LogServerFailure(
        ILogger logger,
        Exception exception,
        string? errorCode,
        int statusCode,
        string? requestPath);

    private static (int? StatusCode, string? Title, string? Code) Map(Exception exception) =>
        exception switch
        {
            ApplicationFailureException { Code: ApplicationErrorCodes.StageNotFound } application =>
                (StatusCodes.Status404NotFound, "Stage not found", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CompetitionNotFound } application =>
                (StatusCodes.Status404NotFound, "Competition not found", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.MatchNotFound } application =>
                (StatusCodes.Status404NotFound, "Match not found", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.MatchOperationNotAllowed } application =>
                (StatusCodes.Status409Conflict, "Match operation not allowed", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ConsequenceOperationNotAllowed } application =>
                (StatusCodes.Status409Conflict, "Consequence operation not allowed", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.SlotOccupancyConflict } application =>
                (StatusCodes.Status409Conflict, "Slot occupancy conflict", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CompletionNotAllowed } application =>
                (StatusCodes.Status409Conflict, "Completion not allowed", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CompetitionClosed } application =>
                (StatusCodes.Status409Conflict, "Competition closed", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.MediaNotFound } application =>
                (StatusCodes.Status400BadRequest, "Media not found", application.Code),
            ApplicationFailureException application =>
                (StatusCodes.Status400BadRequest, "Application failure", application.Code),
            DomainException domain =>
                (StatusCodes.Status409Conflict, "Domain rule violation", domain.Code),
            _ => (null, null, null)
        };
}
