// -----------------------------------------------------------------------
// <copyright file="DeterministicEntropy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Pseudo-random source derived from workspace seed + scenario id (independent per scenario).
/// </summary>
public sealed class DeterministicEntropy
{
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicEntropy"/> class.
    /// </summary>
    /// <param name="scenarioId">Scenario identity.</param>
    /// <param name="workspaceSeed">Workspace seed.</param>
    public DeterministicEntropy(string scenarioId, int workspaceSeed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        var material = Encoding.UTF8.GetBytes($"{scenarioId}\0{workspaceSeed.ToString(CultureInfo.InvariantCulture)}");
        var hash = SHA256.HashData(material);
        var seed = BitConverter.ToInt32(hash, 0);
        _random = new Random(seed);
    }

    /// <summary>
    /// Returns a non-negative random integer strictly less than <paramref name="maxExclusive"/>.
    /// </summary>
    /// <param name="maxExclusive">Exclusive upper bound.</param>
    /// <returns>Random integer.</returns>
#pragma warning disable CA5394 // Deterministic Random is intentional for Development Workspace.
    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    /// <summary>
    /// Returns a random integer in <paramref name="minInclusive"/>..<paramref name="maxInclusive"/>.
    /// </summary>
    /// <param name="minInclusive">Inclusive lower bound.</param>
    /// <param name="maxInclusive">Inclusive upper bound.</param>
    /// <returns>Random integer.</returns>
    public int NextInclusive(int minInclusive, int maxInclusive) =>
        _random.Next(minInclusive, maxInclusive + 1);

    /// <summary>
    /// Returns whether a percentile roll succeeds (0–99 &lt; <paramref name="percent"/>).
    /// </summary>
    /// <param name="percent">Success percentage (0–100).</param>
    /// <returns><see langword="true"/> when the roll is below <paramref name="percent"/>.</returns>
    public bool Chance(int percent) => _random.Next(100) < percent;

    /// <summary>
    /// Fisher–Yates shuffle of <paramref name="items"/> (in place).
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="items">List to shuffle.</param>
    public void Shuffle<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
#pragma warning restore CA5394
}
