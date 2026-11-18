// -----------------------------------------------------------------------
// <copyright file="ManualPlacementSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Decides when a qualification or progression path may be overwritten by hand.
/// The screen uses the same rule: only a definitive source, completed or deleted.
/// A source that is still listed but was not loaded stays reserved.
/// </summary>
public static class ManualPlacementSource
{
    /// <summary>
    /// True when <paramref name="sourceStageId"/> is absent from the competition,
    /// or the loaded source phase is <see cref="StageStatus.Completed"/>.
    /// </summary>
    public static bool AllowsOverwrite(Competition competition, Stage? source, Guid? sourceStageId)
    {
        ArgumentNullException.ThrowIfNull(competition);
        if (sourceStageId is not Guid id)
        {
            return false;
        }

        var listed = competition.StageIds.Any(stageId => stageId.Value == id);
        if (!listed)
        {
            return true;
        }

        return source is { Status: StageStatus.Completed };
    }
}
