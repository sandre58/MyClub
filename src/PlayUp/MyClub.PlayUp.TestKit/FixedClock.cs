// -----------------------------------------------------------------------
// <copyright file="FixedClock.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.TestKit;

/// <summary>
/// Deterministic <see cref="IClock"/> for tests (fixed instant; no wall-clock drift).
/// </summary>
public sealed class FixedClock : IClock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedClock"/> class.
    /// </summary>
    /// <param name="utcNow">Fixed UTC instant.</param>
    public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; }
}
