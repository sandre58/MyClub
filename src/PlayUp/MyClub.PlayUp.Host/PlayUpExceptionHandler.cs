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
            if (application.Code == ApplicationErrorCodes.ReadyConsumerWouldBreak)
            {
                problem.Extensions["consumerStageIds"] = application.Reasons;
            }
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
            ApplicationFailureException { Code: ApplicationErrorCodes.StageCompletionBlocked } application =>
                (StatusCodes.Status409Conflict, "Stage completion blocked", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CompetitionClosed } application =>
                (StatusCodes.Status409Conflict, "Competition closed", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ReadyConsumerWouldBreak } application =>
                (StatusCodes.Status409Conflict, "Ready consumer feed would break", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.AppliedDrawPoolChangeBlocked } application =>
                (StatusCodes.Status409Conflict, "Applied draw pool change blocked", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.AppliedDrawFeedRulesChangeBlocked } application =>
                (StatusCodes.Status409Conflict, "Applied draw feed rules change blocked", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ProgressionFeedConsumed } application =>
                (StatusCodes.Status409Conflict, "Progression feed consumed", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ProgressionFeedImpactAmbiguous } application =>
                (StatusCodes.Status409Conflict, "Progression feed impact ambiguous", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ProgressionDownstreamProductionBlocked } application =>
                (StatusCodes.Status409Conflict, "Progression downstream production blocked", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ProgressionProductionParticipationAmbiguous } application =>
                (StatusCodes.Status409Conflict, "Progression production participation ambiguous", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.AppliedDrawSlotPlacementProtected } application =>
                (StatusCodes.Status409Conflict, "Applied draw slot placement protected", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.AppliedDrawGroupPlacementProtected } application =>
                (StatusCodes.Status409Conflict, "Applied draw group placement protected", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.GroupSeatReservedByInboundFeed } application =>
                (StatusCodes.Status409Conflict, "Group seat reserved by inbound feed", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw } application =>
                (StatusCodes.Status409Conflict, "Manual group placement blocked by active draw", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawObsoleteForConsumption } application =>
                (StatusCodes.Status409Conflict, "Draw obsolete for consumption", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.MaterializationOccupantConflict } application =>
                (StatusCodes.Status409Conflict, "Materialization occupant conflict", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawLaunchGridNotVacant } application =>
                (StatusCodes.Status409Conflict, "Draw launch grid not vacant", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawLaunchDirectAssignmentPresent } application =>
                (StatusCodes.Status409Conflict, "Draw launch blocked by direct assignment", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawNoFreeSlot } application =>
                (StatusCodes.Status409Conflict, "Partial draw has no free slot", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawNotEnoughTeams } application =>
                (StatusCodes.Status409Conflict, "Partial draw does not have enough teams", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawTooManyTeams } application =>
                (StatusCodes.Status409Conflict, "Partial draw has too many teams", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawReleaseBlockedByAttachedMatches } application =>
                (StatusCodes.Status409Conflict, "Draw release blocked by attached matches", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawReleaseRequiresCancelled } application =>
                (StatusCodes.Status409Conflict, "Draw release requires a cancelled draw", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.DrawRedoRequiresApplied } application =>
                (StatusCodes.Status409Conflict, "Draw redo requires an applied draw", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceOccupantNotFound } application =>
                (StatusCodes.Status404NotFound, "Resolution occupant not found", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceConflict } application =>
                (StatusCodes.Status409Conflict, "Cascade confirmation conflict", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceNotReplayable } application =>
                (StatusCodes.Status409Conflict, "Cascade occupant is not replayable", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceNotStale } application =>
                (StatusCodes.Status409Conflict, "Cascade occupant is not stale", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceDirectAssignment } application =>
                (StatusCodes.Status409Conflict, "Direct assignment is not replaced", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceCompleted } application =>
                (StatusCodes.Status409Conflict, "Completed phase blocks the cascade", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceCycle } application =>
                (StatusCodes.Status409Conflict, "Resolution chain contains a cycle", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceIndeterminate } application =>
                (StatusCodes.Status409Conflict, "Cascade selection is indeterminate", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.CascadeReplaceLiveBlocked } application =>
                (StatusCodes.Status409Conflict, "Live match blocks the cascade", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseOccupantNotFound } application =>
                (StatusCodes.Status404NotFound, "Nothing to release", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseConflict } application =>
                (StatusCodes.Status409Conflict, "Resolution release conflict", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseNotReleasable } application =>
                (StatusCodes.Status409Conflict, "Place cannot be released", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseNotIndeterminate } application =>
                (StatusCodes.Status409Conflict, "Place is no longer unconfirmed", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseCompleted } application =>
                (StatusCodes.Status409Conflict, "Completed phase cannot release a place", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseDirectAssignment } application =>
                (StatusCodes.Status409Conflict, "Direct assignment is not released", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseBlockedByPlayedMatch } application =>
                (StatusCodes.Status409Conflict, "Played match blocks the release", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseBlockedByMatches } application =>
                (StatusCodes.Status409Conflict, "Generated matches block the release", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseBlockedByMatchesRunning } application =>
                (StatusCodes.Status409Conflict, "In-progress phase blocks the release", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.ResolutionReleaseBlockedByDraw } application =>
                (StatusCodes.Status409Conflict, "Draw blocks the release", application.Code),
            ApplicationFailureException { Code: ApplicationErrorCodes.MediaNotFound } application =>
                (StatusCodes.Status400BadRequest, "Media not found", application.Code),
            ApplicationFailureException application =>
                (StatusCodes.Status400BadRequest, "Application failure", application.Code),
            DomainException domain =>
                (StatusCodes.Status409Conflict, "Domain rule violation", domain.Code),
            _ => (null, null, null)
        };
}
