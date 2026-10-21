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
/// <remarks>
/// Initializes a new instance of the <see cref="FixedClock"/> class.
/// </remarks>
/// <param name="utcNow">Fixed UTC instant.</param>
public sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; } = utcNow;
}
