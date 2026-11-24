// -----------------------------------------------------------------------
// <copyright file="PlayUpCompetitionLockOptions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;

namespace MyClub.PlayUp.Infrastructure.DependencyInjection;

/// <summary>
/// PostgreSQL <c>lock_timeout</c> for competition exclusive writes.
/// </summary>
public sealed class PlayUpCompetitionLockOptions
{
    /// <summary>
    /// Configuration key for the lock wait duration.
    /// </summary>
    public const string ConfigKey = "PlayUp:CompetitionLockTimeout";

    /// <summary>
    /// Production default: wait up to three seconds for the competition advisory lock.
    /// </summary>
    public static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Host integration tests allow twenty seconds so overlap harnesses can hold a session lock for five seconds.
    /// </summary>
    public static readonly TimeSpan HostIntegrationLockTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Upper bound for configured lock timeouts.
    /// </summary>
    public static readonly TimeSpan MaximumLockTimeout = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets the built-in default used when configuration does not override the timeout.
    /// </summary>
    public static PlayUpCompetitionLockOptions Default { get; } = new();

    /// <summary>
    /// Gets the duration passed to <c>SET LOCAL lock_timeout</c>.
    /// </summary>
    public TimeSpan LockTimeout { get; init; } = DefaultLockTimeout;

    /// <summary>
    /// Parses configuration text into validated options. Null or whitespace yields the production default.
    /// </summary>
    /// <param name="configurationValue">Raw configuration value (time span or integer milliseconds).</param>
    /// <returns>Validated options.</returns>
    public static PlayUpCompetitionLockOptions Parse(string? configurationValue)
    {
        if (string.IsNullOrWhiteSpace(configurationValue))
        {
            return Default;
        }

        var trimmed = configurationValue.Trim();
        TimeSpan timeout;
        if (int.TryParse(trimmed, out var milliseconds))
        {
            if (milliseconds <= 0)
            {
                throw new InvalidOperationException(
                    $"PlayUp:CompetitionLockTimeout must be a strictly positive duration. '{configurationValue}' is not valid.");
            }

            timeout = TimeSpan.FromMilliseconds(milliseconds);
        }
        else if (!TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out timeout))
        {
            throw new InvalidOperationException(
                $"PlayUp:CompetitionLockTimeout must be a time span or a positive integer of milliseconds. '{configurationValue}' is not valid.");
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "PlayUp:CompetitionLockTimeout must be strictly positive. PostgreSQL lock_timeout = 0 means no waiting.");
        }

        if (timeout > MaximumLockTimeout)
        {
            throw new InvalidOperationException(
                $"PlayUp:CompetitionLockTimeout must not exceed {MaximumLockTimeout.TotalSeconds:0} seconds.");
        }

        return new PlayUpCompetitionLockOptions { LockTimeout = timeout };
    }

    /// <summary>
    /// Refuses startup when the lock timeout would exceed or equal the Npgsql command timeout.
    /// </summary>
    /// <param name="npgsqlCommandTimeoutSeconds">The EF/Npgsql command timeout in seconds.</param>
    public void ValidateAgainstCommandTimeout(int npgsqlCommandTimeoutSeconds)
    {
        if (npgsqlCommandTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Npgsql CommandTimeout must be positive.");
        }

        if (LockTimeout >= TimeSpan.FromSeconds(npgsqlCommandTimeoutSeconds))
        {
            throw new InvalidOperationException(
                "PlayUp:CompetitionLockTimeout must be strictly less than the Npgsql CommandTimeout.");
        }
    }

    /// <summary>
    /// PostgreSQL-safe literal for <c>SET LOCAL lock_timeout</c> (integer milliseconds only).
    /// </summary>
    /// <returns>A duration literal such as <c>3000ms</c>.</returns>
    internal string ToPostgresLockTimeoutLiteral()
    {
        var milliseconds = checked((long)LockTimeout.TotalMilliseconds);
        return milliseconds <= 0 ? throw new InvalidOperationException("PlayUp competition lock timeout must be strictly positive.") : FormattableString.Invariant($"{milliseconds}ms");
    }
}
