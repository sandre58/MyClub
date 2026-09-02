// -----------------------------------------------------------------------
// <copyright file="SquadGenerator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Development.Generators;

/// <summary>
/// Resolves a roster for a seeded team: catalog overlay when present, else generated names.
/// </summary>
public static class SquadGenerator
{
    /// <summary>Number of generated players when no catalog overlay exists.</summary>
    public const int GeneratedPlayerCount = 16;

    private static readonly int[] TypicalJerseys =
    [
        1, 16, 2, 3, 4, 5, 6, 8, 10, 7, 9, 11, 12, 14, 17, 18, 20, 21, 22, 23, 24, 27, 28, 29, 30
    ];

    /// <summary>
    /// Builds the declared roster for a team index / display name.
    /// </summary>
    /// <param name="teamIndex">Zero-based team index in the competition.</param>
    /// <param name="displayName">Team display name (catalog key).</param>
    /// <param name="catalog">Optional catalog (defaults to <see cref="SquadCatalog.Instance"/>).</param>
    /// <param name="lexicon">Optional name lexicon for generated squads.</param>
    /// <returns>Players then staff.</returns>
    public static IReadOnlyList<GeneratedSquadMember> ForTeam(
        int teamIndex,
        string displayName,
        SquadCatalog? catalog = null,
        GeneratedPlayerNameLexicon? lexicon = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentOutOfRangeException.ThrowIfNegative(teamIndex);

        return (catalog ?? SquadCatalog.Instance).TryGet(displayName, out var overlay)
            ? overlay
            : Generate(lexicon ?? GeneratedPlayerNameLexicon.Instance, teamIndex, displayName);
    }

    /// <summary>
    /// Resolves the jersey used on seeded match sheets for a declared player.
    /// </summary>
    /// <param name="entry">Owning entry.</param>
    /// <param name="member">Declared player.</param>
    /// <returns>Jersey number when known.</returns>
    public static int? ResolveJersey(CompetitionEntry entry, DeclaredMember member)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(member);

        if (SquadCatalog.Instance.TryGet(entry.DisplayName, out var squad))
        {
            return squad.FirstOrDefault(item =>
                    item.Role == DeclaredMemberRole.Player
                    && string.Equals(item.DisplayName, member.DisplayName, StringComparison.Ordinal))
                ?.JerseyNumber;
        }

        var index = 0;
        foreach (var candidate in entry.DeclaredMembers)
        {
            if (candidate.Role != DeclaredMemberRole.Player)
            {
                continue;
            }

            if (candidate.Id.Equals(member.Id))
            {
                return index < TypicalJerseys.Length ? TypicalJerseys[index] : index + 1;
            }

            index++;
        }

        return null;
    }

    private static List<GeneratedSquadMember> Generate(
        GeneratedPlayerNameLexicon lexicon,
        int teamIndex,
        string displayName)
    {
        var members = new List<GeneratedSquadMember>(GeneratedPlayerCount + 1);
        var used = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < GeneratedPlayerCount; i++)
        {
            var name = UniqueName(lexicon, teamIndex, i, used);
            var jersey = i < TypicalJerseys.Length ? TypicalJerseys[i] : i + 1;
            members.Add(new GeneratedSquadMember(name, DeclaredMemberRole.Player, jersey));
        }

        members.Add(new GeneratedSquadMember($"Staff {Truncate(displayName, 80)}", DeclaredMemberRole.Staff, null));
        return members;
    }

    private static string UniqueName(
        GeneratedPlayerNameLexicon lexicon,
        int teamIndex,
        int playerIndex,
        HashSet<string> used)
    {
        var firstCount = lexicon.FirstNames.Count;
        var lastCount = lexicon.LastNames.Count;
        for (var bump = 0; bump < firstCount * lastCount; bump++)
        {
            var first = lexicon.FirstNames[(teamIndex + playerIndex + bump) % firstCount];
            var last = lexicon.LastNames[((teamIndex * 13) + (playerIndex * 7) + bump) % lastCount];
            var name = $"{first} {last}";
            if (used.Add(name))
            {
                return name;
            }
        }

        return $"Joueur {teamIndex + 1}-{playerIndex + 1}";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
