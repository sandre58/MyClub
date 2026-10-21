// -----------------------------------------------------------------------
// <copyright file="GeneratedPlayerNameLexicon.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;

namespace MyClub.PlayUp.Development.Generators;

/// <summary>
/// First / last names for deterministic generated squads when no catalog overlay exists.
/// Loaded from embedded <c>generated-player-names.json</c>.
/// </summary>
public sealed class GeneratedPlayerNameLexicon
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<GeneratedPlayerNameLexicon> Shared = new(
        static () => LoadFromAssembly(typeof(GeneratedPlayerNameLexicon).Assembly));

    private GeneratedPlayerNameLexicon(IReadOnlyList<string> firstNames, IReadOnlyList<string> lastNames)
    {
        FirstNames = firstNames;
        LastNames = lastNames;
    }

    /// <summary>Gets the shared lexicon loaded from the Development assembly.</summary>
    public static GeneratedPlayerNameLexicon Instance => Shared.Value;

    /// <summary>Gets given names.</summary>
    public IReadOnlyList<string> FirstNames { get; }

    /// <summary>Gets family names.</summary>
    public IReadOnlyList<string> LastNames { get; }

    /// <summary>
    /// Loads the embedded player-name lexicon from the given assembly.
    /// </summary>
    /// <param name="assembly">Assembly containing the embedded JSON.</param>
    /// <returns>Lexicon instance.</returns>
    public static GeneratedPlayerNameLexicon LoadFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        const string marker = ".Generators.generated-player-names.json";
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(marker, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "Embedded resource 'generated-player-names.json' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        var document = JsonSerializer.Deserialize<PlayerNameDocument>(reader.ReadToEnd(), JsonOptions)
            ?? throw new InvalidOperationException($"Invalid generator JSON '{resourceName}'.");

        return document.FirstNames.Count == 0 || document.LastNames.Count == 0
            ? throw new InvalidOperationException(
                "generated-player-names.json must define non-empty firstNames and lastNames.")
            : new GeneratedPlayerNameLexicon(document.FirstNames, document.LastNames);
    }

    private sealed class PlayerNameDocument
    {
        public List<string> FirstNames { get; init; } = [];

        public List<string> LastNames { get; init; } = [];
    }
}
