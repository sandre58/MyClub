// -----------------------------------------------------------------------
// <copyright file="DatasetCatalog.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;

namespace MyClub.PlayUp.Development.Datasets;

/// <summary>
/// Loads embedded JSON team datasets for Development seeding.
/// </summary>
public sealed class DatasetCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, DatasetDocument> _byKey;

    private DatasetCatalog(Dictionary<string, DatasetDocument> byKey) =>
        _byKey = byKey;

    /// <summary>Gets all loaded datasets.</summary>
    public IReadOnlyCollection<DatasetDocument> All => _byKey.Values;

    /// <summary>
    /// Loads every <c>*.json</c> embedded resource under the Datasets namespace.
    /// </summary>
    /// <param name="assembly">Assembly containing embedded datasets.</param>
    /// <returns>Catalog instance.</returns>
    public static DatasetCatalog LoadFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var byKey = new Dictionary<string, DatasetDocument>(StringComparer.OrdinalIgnoreCase);
        const string marker = ".Datasets.";
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.Contains(marker, StringComparison.Ordinal)
                || !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var document = JsonSerializer.Deserialize<DatasetDocument>(json, JsonOptions)
                ?? throw new InvalidOperationException($"Invalid dataset JSON '{resourceName}'.");
            if (string.IsNullOrWhiteSpace(document.Key))
            {
                throw new InvalidOperationException($"Dataset '{resourceName}' has no key.");
            }

            if (document.Teams.Count == 0)
            {
                throw new InvalidOperationException($"Dataset '{document.Key}' has no teams.");
            }

            if (!byKey.TryAdd(document.Key, document))
            {
                throw new InvalidOperationException($"Duplicate dataset key '{document.Key}'.");
            }
        }

        return byKey.Count == 0 ? throw new InvalidOperationException("No Development datasets were embedded.") : new DatasetCatalog(byKey);
    }

    /// <summary>
    /// Resolves a dataset by key (also accepts legacy aliases).
    /// </summary>
    /// <param name="key">Dataset key.</param>
    /// <returns>Dataset document.</returns>
    public DatasetDocument Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var normalized = NormalizeKey(key);
        return _byKey.TryGetValue(normalized, out var document)
            ? document
            : throw new InvalidOperationException($"Unknown dataset key '{key}'.");
    }

    /// <summary>
    /// Returns ordered team rows for a dataset key.
    /// </summary>
    /// <param name="key">Dataset key.</param>
    /// <returns>Team rows.</returns>
    public IReadOnlyList<DatasetTeamDocument> GetTeams(string key) => Get(key).Teams;

    private static string NormalizeKey(string key)
    {
        var trimmed = key.Trim();
        return trimmed;
    }
}
