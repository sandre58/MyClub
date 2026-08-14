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
internal sealed class PlayUpExceptionHandler : IExceptionHandler
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
            ApplicationFailureException { Code: ApplicationErrorCodes.StageNotFound } application =>
                (StatusCodes.Status404NotFound, "Stage not found", application.Code),
            ApplicationFailureException application =>
                (StatusCodes.Status400BadRequest, "Application failure", application.Code),
            DomainException domain =>
                (StatusCodes.Status409Conflict, "Domain rule violation", domain.Code),
            _ => (null, null, null)
        };
}
