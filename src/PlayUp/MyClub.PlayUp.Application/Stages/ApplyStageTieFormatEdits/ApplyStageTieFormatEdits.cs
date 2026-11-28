// -----------------------------------------------------------------------
// <copyright file="ApplyStageTieFormatEdits.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages.ApplyStageTieFormatEdits;

/// <summary>
/// Application use case: atomically apply default and per-round TieFormat edits.
/// </summary>
public static class ApplyStageTieFormatEdits
{
    /// <summary>
    /// Applies default and round tie-format changes in one transaction.
    /// Reverts Ready to Draft at most once before structural edits.
    /// </summary>
    public static void Execute(
        Stage stage,
        Competition competition,
        TieFormat? defaultTieFormat,
        bool replaceDefault,
        IReadOnlyList<StageTieFormatRoundEdit> roundEdits,
        IReadOnlyList<Match> stageMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(roundEdits);
        ArgumentNullException.ThrowIfNull(stageMatches);
        ArgumentNullException.ThrowIfNull(clock);

        StageStructureEditGuard.EnsureDraftHeavyStructureAllowed(stage, competition);

        foreach (var edit in roundEdits)
        {
            if (stage.Rounds.All(round => !round.Id.Equals(edit.RoundId)))
            {
                throw new DomainException(
                    $"Round '{edit.RoundId}' was not found.",
                    StageErrorCodes.RoundNotFound);
            }
        }

        if ((replaceDefault || roundEdits.Count > 0) && stage.Status is StageStatus.Ready)
        {
            RevertToDraft.Execute(stage, stageMatches, clock);
        }

        if (replaceDefault)
        {
            stage.ReplaceDefaultTieFormat(defaultTieFormat, clock);
        }

        foreach (var edit in roundEdits)
        {
            stage.ReplaceRoundTieFormat(edit.RoundId, edit.TieFormat, clock);
        }
    }
}
