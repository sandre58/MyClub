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
/// Deterministic team display name / presentation generator.
/// </summary>
public static class TeamNameGenerator
{
    private static readonly Lazy<GeneratedTeamNameLexicon> Lexicon = new(
        static () => GeneratedTeamNameLexicon.LoadFromAssembly(typeof(TeamNameGenerator).Assembly));

    /// <summary>
    /// Resolves display name and presentation for a team index.
    /// </summary>
    public static (string DisplayName, EntryPresentation Presentation) CreatePresentation(
        DeterministicEntropy entropy,
        int index,
        TeamNameSource source = TeamNameSource.Generated,
        string? datasetKey = null,
        DatasetCatalog? datasets = null)
    {
        ArgumentNullException.ThrowIfNull(entropy);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (source == TeamNameSource.Dataset)
        {
            var row = ResolveDatasetRow(datasets, datasetKey, index);
            var displayName = Truncate(row.DisplayName);
            return (
                displayName,
                new EntryPresentation(
                    ShortName.Create(row.ShortName),
                    LogoUri.Create(row.LogoPath),
                    TeamColor.Create(row.PrimaryColor),
                    TeamColor.Create(row.SecondaryColor)));
        }

        var lexicon = Lexicon.Value;
        var generated = Truncate(Generate(lexicon, index));
        return (
            generated,
            new EntryPresentation(
                ShortName.Create(DeriveShortName(generated)),
                null,
                TeamColor.Create(lexicon.Palette[index % lexicon.Palette.Count]),
                TeamColor.Create(lexicon.Palette[(index + 3) % lexicon.Palette.Count])));
    }

    /// <summary>
    /// Generates or resolves a team display name.
    /// </summary>
    public static string Create(
        DeterministicEntropy entropy,
        int index,
        TeamNameSource source = TeamNameSource.Generated,
        string? datasetKey = null,
        DatasetCatalog? datasets = null) =>
        CreatePresentation(entropy, index, source, datasetKey, datasets).DisplayName;

    private static DatasetTeamDocument ResolveDatasetRow(DatasetCatalog? datasets, string? datasetKey, int index)
    {
        ArgumentNullException.ThrowIfNull(datasets);
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetKey);
        var list = datasets.GetTeams(datasetKey);
        return index >= list.Count
            ? throw new InvalidOperationException(
                $"Dataset '{datasetKey}' has {list.Count} clubs; index {index} is out of range.")
            : list[index];
    }

    private static string Generate(GeneratedTeamNameLexicon lexicon, int index)
    {
        var prefix = lexicon.Prefixes[index % lexicon.Prefixes.Count];
        var place = lexicon.Places[(index * 7) % lexicon.Places.Count];
        return $"{prefix} {place}";
    }

    private static string DeriveShortName(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        switch (parts.Length)
        {
            case 0:
                return "T";
            case 1:
                return parts[0].Length <= ShortName.MaxLength
                    ? parts[0].ToUpperInvariant()
                    : parts[0][..ShortName.MaxLength].ToUpperInvariant();
            default:
                {
                    var initials = string.Concat(parts.Take(3).Select(part => char.ToUpperInvariant(part[0])));
                    return initials;
                }
        }
    }

    private static string Truncate(string name) =>
        name.Length <= CompetitionEntry.DisplayNameMaxLength
            ? name
            : name[..CompetitionEntry.DisplayNameMaxLength];
}
