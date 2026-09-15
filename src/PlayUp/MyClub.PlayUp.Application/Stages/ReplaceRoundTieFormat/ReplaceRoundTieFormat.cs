// -----------------------------------------------------------------------
// <copyright file="ReplaceRoundTieFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: replace or clear a Round's TieFormat.
/// </summary>
public static class ReplaceRoundTieFormat
{
    /// <summary>
    /// Replaces the round tie format (<see langword="null"/> clears → effective OneLeg).
    /// </summary>
    public static void Execute(Stage stage, RoundId roundId, TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        stage.ReplaceRoundTieFormat(roundId, tieFormat, clock);
    }
}
