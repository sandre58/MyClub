// -----------------------------------------------------------------------
// <copyright file="MatchRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// How an individual match is played (duration and optional policies).
/// Optional policies use presence for enabled and <see langword="null"/> for disabled.
/// </summary>
public sealed record MatchRules
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRules"/> class.
    /// </summary>
    /// <param name="duration">Regulation-time duration.</param>
    /// <param name="administrativeResultPolicy">Default administrative / forfeit score policy.</param>
    /// <param name="extraTimePolicy">Extra time when enabled; <see langword="null"/> when disabled.</param>
    /// <param name="penaltyShootoutPolicy">Penalty shootout when enabled; <see langword="null"/> when disabled.</param>
    public MatchRules(
        MatchDuration duration,
        AdministrativeResultPolicy administrativeResultPolicy,
        ExtraTimePolicy? extraTimePolicy = null,
        PenaltyShootoutPolicy? penaltyShootoutPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(duration);
        ArgumentNullException.ThrowIfNull(administrativeResultPolicy);

        Duration = duration;
        AdministrativeResultPolicy = administrativeResultPolicy;
        ExtraTimePolicy = extraTimePolicy;
        PenaltyShootoutPolicy = penaltyShootoutPolicy;
    }

    /// <summary>
    /// Gets the regulation-time duration.
    /// </summary>
    public MatchDuration Duration { get; }

    /// <summary>
    /// Gets the administrative result policy (required).
    /// </summary>
    public AdministrativeResultPolicy AdministrativeResultPolicy { get; }

    /// <summary>
    /// Gets the extra-time policy when enabled; otherwise <see langword="null"/>.
    /// </summary>
    public ExtraTimePolicy? ExtraTimePolicy { get; }

    /// <summary>
    /// Gets the penalty shootout policy when enabled; otherwise <see langword="null"/>.
    /// </summary>
    public PenaltyShootoutPolicy? PenaltyShootoutPolicy { get; }
}
