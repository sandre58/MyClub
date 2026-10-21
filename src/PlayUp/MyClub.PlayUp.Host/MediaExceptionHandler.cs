// -----------------------------------------------------------------------
// <copyright file="MediaExceptionHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyClub.Media.Application;
using MediaDomainException = MyClub.Media.Domain.DomainException;

namespace MyClub.PlayUp.Host;

/// <summary>
/// Maps Media Domain / Application failures to ProblemDetails (composition root for Media capability).
/// </summary>
/// <param name="logger">Logger for server failures (≥ 500).</param>
internal sealed partial class MediaExceptionHandler(ILogger<MediaExceptionHandler> logger) : IExceptionHandler
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
            ApplicationFailureException { Code: MediaApplicationErrorCodes.MediaNotFound } application =>
                (StatusCodes.Status404NotFound, "Media not found", application.Code),
            ApplicationFailureException { Code: MediaApplicationErrorCodes.ContentUnavailable } application =>
                (StatusCodes.Status404NotFound, "Media content unavailable", application.Code),
            ApplicationFailureException { Code: MediaApplicationErrorCodes.StorageDeleteFailed } application =>
                (StatusCodes.Status500InternalServerError, "Media storage delete failed", application.Code),
            ApplicationFailureException application =>
                (StatusCodes.Status400BadRequest, "Media application failure", application.Code),
            MediaDomainException domain =>
                (StatusCodes.Status400BadRequest, "Media rule violation", domain.Code),
            _ => (null, null, null)
        };
}
