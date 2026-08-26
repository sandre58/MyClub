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
internal sealed class MediaExceptionHandler : IExceptionHandler
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

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        httpContext.Response.StatusCode = statusCode.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
        return true;
    }

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
