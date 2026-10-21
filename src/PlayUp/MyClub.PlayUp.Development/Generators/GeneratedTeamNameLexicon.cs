// -----------------------------------------------------------------------
// <copyright file="GeneratedTeamNameLexicon.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;

namespace MyClub.PlayUp.Development.Generators;

/// <summary>
/// Prefixes, places, and colors for deterministic generated team presentation.
/// Loaded from embedded <c>generated-team-names.json</c>.
/// </summary>
public sealed class GeneratedTeamNameLexicon
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private GeneratedTeamNameLexicon(
        IReadOnlyList<string> prefixes,
        IReadOnlyList<string> places,
        IReadOnlyList<string> palette)
    {
        Prefixes = prefixes;
        Places = places;
        Palette = palette;
    }

    /// <summary>Gets club name prefixes (e.g. FC, Olympique).</summary>
    public IReadOnlyList<string> Prefixes { get; }

    /// <summary>Gets place / region suffixes.</summary>
    public IReadOnlyList<string> Places { get; }

    /// <summary>Gets hex color palette for primary/secondary colors.</summary>
    public IReadOnlyList<string> Palette { get; }

    /// <summary>
    /// Loads the embedded generator lexicon from the Development assembly.
    /// </summary>
    /// <param name="assembly">Assembly containing the embedded JSON.</param>
    /// <returns>Lexicon instance.</returns>
    public static GeneratedTeamNameLexicon LoadFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        const string marker = ".Generators.generated-team-names.json";
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(marker, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "Embedded resource 'generated-team-names.json' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        var document = JsonSerializer.Deserialize<GeneratedTeamNameDocument>(reader.ReadToEnd(), JsonOptions)
            ?? throw new InvalidOperationException($"Invalid generator JSON '{resourceName}'.");

        return document.Prefixes.Count == 0
            || document.Places.Count == 0
            || document.Palette.Count == 0
            ? throw new InvalidOperationException(
                "generated-team-names.json must define non-empty prefixes, places, and palette.")
            : new GeneratedTeamNameLexicon(document.Prefixes, document.Places, document.Palette);
    }

    private sealed class GeneratedTeamNameDocument
    {
        public List<string> Prefixes { get; init; } = [];

        public List<string> Places { get; init; } = [];

        public List<string> Palette { get; init; } = [];
    }
}
