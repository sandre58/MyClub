// -----------------------------------------------------------------------
// <copyright file="ControlledClock.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Mutable <see cref="IClock"/> for scenario scripts.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ControlledClock"/> class.
/// </remarks>
/// <param name="utcNow">Initial UTC timestamp.</param>
public sealed class ControlledClock(DateTimeOffset utcNow) : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    /// <summary>
    /// Advances the clock by the given delta.
    /// </summary>
    /// <param name="delta">Time to add.</param>
    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}
