// -----------------------------------------------------------------------
// <copyright file="SquadCatalog.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Development.Generators;

/// <summary>
/// Named squad overlays for inspired dataset clubs / national teams.
/// Loaded from embedded <c>squads.json</c> (display name → staff + jerseyed players).
/// </summary>
public sealed class SquadCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<SquadCatalog> Shared = new(
        static () => LoadFromAssembly(typeof(SquadCatalog).Assembly));

    private readonly IReadOnlyDictionary<string, IReadOnlyList<GeneratedSquadMember>> _byDisplayName;

    private SquadCatalog(IReadOnlyDictionary<string, IReadOnlyList<GeneratedSquadMember>> byDisplayName) =>
        _byDisplayName = byDisplayName;

    /// <summary>Gets the shared catalog loaded from the Development assembly.</summary>
    public static SquadCatalog Instance => Shared.Value;

    /// <summary>Gets the team display names that have a catalog overlay.</summary>
    public IReadOnlyCollection<string> TeamNames => [.._byDisplayName.Keys];

    /// <summary>
    /// Loads the embedded squad catalog from the given assembly.
    /// </summary>
    /// <param name="assembly">Assembly containing the embedded JSON.</param>
    /// <returns>Catalog instance.</returns>
    public static SquadCatalog LoadFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        const string marker = ".Generators.squads.json";
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(marker, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Embedded resource 'squads.json' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        var document = JsonSerializer.Deserialize<Dictionary<string, SquadJson>>(reader.ReadToEnd(), JsonOptions)
            ?? throw new InvalidOperationException($"Invalid squads JSON '{resourceName}'.");

        var byName = new Dictionary<string, IReadOnlyList<GeneratedSquadMember>>(StringComparer.Ordinal);
        foreach (var (displayName, squad) in document)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new InvalidOperationException("squads.json contains a blank team name.");
            }

            var members = new List<GeneratedSquadMember>();
            foreach (var (jerseyText, playerName) in squad.Players)
            {
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    throw new InvalidOperationException($"Squad '{displayName}' has a blank player name.");
                }

                var jersey = int.TryParse(jerseyText, out var parsed) ? parsed : (int?)null;
                if (jersey is { } number
                    && members.Exists(item => item.Role == DeclaredMemberRole.Player && item.JerseyNumber == number))
                {
                    throw new InvalidOperationException(
                        $"Squad '{displayName}' has duplicate jersey '{number}'.");
                }

                if (members.Exists(item =>
                        string.Equals(item.DisplayName, playerName.Trim(), StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException(
                        $"Squad '{displayName}' has duplicate member '{playerName}'.");
                }

                members.Add(new GeneratedSquadMember(playerName.Trim(), DeclaredMemberRole.Player, jersey));
            }

            if (members.Count < 11)
            {
                throw new InvalidOperationException(
                    $"Squad '{displayName}' must declare at least 11 players (found {members.Count}).");
            }

            members.AddRange(from staffName in squad.Staff where !string.IsNullOrWhiteSpace(staffName) select new GeneratedSquadMember(staffName.Trim(), DeclaredMemberRole.Staff, null));

            byName[displayName] = new ReadOnlyCollection<GeneratedSquadMember>(members);
        }

        return new SquadCatalog(byName);
    }

    /// <summary>
    /// Tries to resolve a catalog overlay for a team display name.
    /// </summary>
    /// <param name="displayName">Team display name.</param>
    /// <param name="members">Resolved members when found.</param>
    /// <returns><see langword="true"/> when an overlay exists.</returns>
    public bool TryGet(string displayName, out IReadOnlyList<GeneratedSquadMember> members)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return _byDisplayName.TryGetValue(displayName, out members!);
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Used by Serialization")]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local", Justification = "Bound from JSON")]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local", Justification = "Bound from JSON")]
    private sealed class SquadJson
    {
#pragma warning disable CA2227 // Bound from JSON.
        public List<string> Staff { get; set; } = [];

        public Dictionary<string, string> Players { get; set; } = new(StringComparer.Ordinal);
#pragma warning restore CA2227
    }
}
