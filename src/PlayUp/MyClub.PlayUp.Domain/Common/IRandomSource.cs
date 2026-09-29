// -----------------------------------------------------------------------
// <copyright file="IRandomSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Non-cryptographic random source for Domain engines (e.g. draw generation).
/// Implementations live in Application/Infrastructure — Domain depends only on this port.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Returns a random integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
    /// </summary>
    /// <param name="minInclusive">Inclusive lower bound.</param>
    /// <param name="maxExclusive">Exclusive upper bound.</param>
    /// <returns>A random integer in the requested range.</returns>
    int NextInt32(int minInclusive, int maxExclusive);

    /// <summary>
    /// Returns a random floating-point number in [0.0, 1.0).
    /// </summary>
    /// <returns>A random double in [0.0, 1.0).</returns>
    double NextDouble();

    /// <summary>
    /// Fills <paramref name="buffer"/> with random bytes.
    /// </summary>
    /// <param name="buffer">Buffer to fill.</param>
    void NextBytes(byte[] buffer);
}
