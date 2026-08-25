// -----------------------------------------------------------------------
// <copyright file="TeamNameGenerator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Datasets;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Development.Generators;

/// <summary>
/// Deterministic team display name generator.
/// </summary>
public static class TeamNameGenerator
{
    private static readonly string[] Prefixes =
    [
        "FC", "AS", "Olympique", "US", "ES", "Stade", "Racing", "Union",
    ];

    private static readonly string[] Places =
    [
        "Valbonne", "Riviera", "du Var", "Méditerranée", "Provence", "Côte Bleue",
        "Aix", "Antibes", "Cannes", "Nice", "Toulon", "Avignon", "Arles", "Gap",
        "Digne", "Sisteron", "Manosque", "Martigues", "Istres", "Salon",
        "Hyères", "Fréjus", "Draguignan", "Grasse", "Menton", "Vence",
        "Cassis", "La Ciotat", "Aubagne", "Vitrolles", "Gardanne", "Pertuis",
    ];

    /// <summary>
    /// Generates or resolves a team display name.
    /// </summary>
    /// <param name="entropy">Entropy source.</param>
    /// <param name="index">Zero-based team index.</param>
    /// <param name="source">Name source.</param>
    /// <param name="datasetKey">Dataset key when <paramref name="source"/> is Dataset.</param>
    /// <param name="datasets">Dataset catalog when resolving Dataset names.</param>
    /// <returns>Display name.</returns>
    public static string Create(
        DeterministicEntropy entropy,
        int index,
        TeamNameSource source = TeamNameSource.Generated,
        string? datasetKey = null,
        DatasetCatalog? datasets = null)
    {
        ArgumentNullException.ThrowIfNull(entropy);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var name = source switch
        {
            TeamNameSource.Dataset => ResolveDataset(datasets, datasetKey, index),
            _ => Generate(index),
        };

        return name.Length <= CompetitionEntry.DisplayNameMaxLength
            ? name
            : name[..CompetitionEntry.DisplayNameMaxLength];
    }

    private static string ResolveDataset(DatasetCatalog? datasets, string? datasetKey, int index)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetKey);
        var list = datasets.GetTeams(datasetKey);
        if (index >= list.Count)
        {
            throw new InvalidOperationException(
                $"Dataset '{datasetKey}' has {list.Count} clubs; index {index} is out of range.");
        }

        return list[index];
    }

    private static string Generate(int index)
    {
        var prefix = Prefixes[index % Prefixes.Length];
        var place = Places[(index * 7) % Places.Length];
        return $"{prefix} {place}";
    }
}
