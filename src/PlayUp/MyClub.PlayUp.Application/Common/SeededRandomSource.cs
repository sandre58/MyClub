// -----------------------------------------------------------------------
// <copyright file="SeededRandomSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using MyNet.Generator;

namespace MyClub.PlayUp.Application;

/// <summary>
/// Application/Infra <see cref="IRandomSource"/> backed by a seeded <see cref="Random"/>.
/// Must not be referenced from Domain (Domain knows only <see cref="IRandomSource"/>).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SeededRandomSource"/> class.
/// </remarks>
/// <param name="seed">Seed for reproducible sequences.</param>
[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Non-cryptographic draw reproducibility.")]
public sealed class SeededRandomSource(int seed) : IRandomSource
{
    private readonly Random _random = new(seed);

    /// <inheritdoc />
    public int NextInt32(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    /// <inheritdoc />
    public double NextDouble() => _random.NextDouble();

    /// <inheritdoc />
    public void NextBytes(byte[] buffer) => _random.NextBytes(buffer);
}
