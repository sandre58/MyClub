// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Logging.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Pipeline;

/// <content>
/// Structured lifecycle log messages for <see cref="UseCaseExecutor"/>.
/// </content>
public sealed partial class UseCaseExecutor
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Competition created {CompetitionId}")]
    private static partial void LogCompetitionCreated(ILogger logger, Guid competitionId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Competition prepared {CompetitionId}")]
    private static partial void LogCompetitionPrepared(ILogger logger, Guid competitionId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Competition started {CompetitionId}")]
    private static partial void LogCompetitionStarted(ILogger logger, Guid competitionId);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Competition completed {CompetitionId} {CompletionMode}")]
    private static partial void LogCompetitionCompleted(ILogger logger, Guid competitionId, CompletionMode completionMode);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Competition archived {CompetitionId}")]
    private static partial void LogCompetitionArchived(ILogger logger, Guid competitionId);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Information,
        Message = "Stage prepared {StageId} {CompetitionId}")]
    private static partial void LogStagePrepared(ILogger logger, Guid stageId, Guid competitionId);

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Information,
        Message = "Stage started {StageId} {CompetitionId}")]
    private static partial void LogStageStarted(ILogger logger, Guid stageId, Guid competitionId);

    [LoggerMessage(
        EventId = 1020,
        Level = LogLevel.Information,
        Message = "Draw published {StageId} {DrawId} {CompetitionId}")]
    private static partial void LogDrawPublished(ILogger logger, Guid stageId, Guid drawId, Guid competitionId);

    [LoggerMessage(
        EventId = 1021,
        Level = LogLevel.Information,
        Message = "Draw applied {StageId} {DrawId} {CompetitionId} {CreatedMatchCount}")]
    private static partial void LogDrawApplied(
        ILogger logger,
        Guid stageId,
        Guid drawId,
        Guid competitionId,
        int createdMatchCount);

    [LoggerMessage(
        EventId = 1030,
        Level = LogLevel.Information,
        Message = "Match started {MatchId} {CompetitionId}")]
    private static partial void LogMatchStarted(ILogger logger, Guid matchId, Guid competitionId);

    [LoggerMessage(
        EventId = 1031,
        Level = LogLevel.Information,
        Message = "Match finished {MatchId} {CompetitionId}")]
    private static partial void LogMatchFinished(ILogger logger, Guid matchId, Guid competitionId);
}
