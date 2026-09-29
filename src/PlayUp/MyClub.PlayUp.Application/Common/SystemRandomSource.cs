// -----------------------------------------------------------------------
// <copyright file="SystemRandomSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application;

/// <summary>
/// Application <see cref="IRandomSource"/> backed by <see cref="Random.Shared"/>.
/// </summary>
[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Non-cryptographic draw generation.")]
public sealed class SystemRandomSource : IRandomSource
{
    /// <inheritdoc />
    public int NextInt32(int minInclusive, int maxExclusive) =>
        Random.Shared.Next(minInclusive, maxExclusive);

    /// <inheritdoc />
    public double NextDouble() => Random.Shared.NextDouble();

    /// <inheritdoc />
    public void NextBytes(byte[] buffer) => Random.Shared.NextBytes(buffer);
}
