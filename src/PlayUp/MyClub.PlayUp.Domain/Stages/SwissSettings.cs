// -----------------------------------------------------------------------
// <copyright file="SwissSettings.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Configuration for a Swiss Kind stage (V1: planned round count K).
/// Presence of settings marks the stage as Swiss — not a Championship RR variant.
/// </summary>
public sealed record SwissSettings
{
    /// <summary>
    /// Minimum planned Swiss rounds.
    /// </summary>
    public const int MinRoundCount = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwissSettings"/> class.
    /// </summary>
    /// <param name="roundCount">Planned number of Swiss rounds (K).</param>
    public SwissSettings(int roundCount)
    {
        if (roundCount < MinRoundCount)
        {
            throw new DomainException(
                $"Swiss round count must be at least {MinRoundCount}.",
                StageErrorCodes.SwissSettingsInvalid);
        }

        RoundCount = roundCount;
    }

    /// <summary>
    /// Gets the planned number of Swiss rounds (K).
    /// </summary>
    public int RoundCount { get; }
}
