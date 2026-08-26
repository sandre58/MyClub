// -----------------------------------------------------------------------
// <copyright file="SeedLogoImporter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using MyClub.Media.Application.Media;
using MyClub.Media.Domain;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Imports Development seed logo assets into Media once per normalized path (CdF/L1 dedup).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SeedLogoImporter"/> class.
/// </remarks>
/// <param name="media">Media application service.</param>
public sealed class SeedLogoImporter(MediaService media)
{
    private static readonly string AssetsRoot = Path.Combine(
        AppContext.BaseDirectory,
        "Assets",
        "seed-logos");

    private readonly MediaService _media = media ?? throw new ArgumentNullException(nameof(media));
    private readonly ConcurrentDictionary<string, LogoMediaId> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns an existing Media id for the asset path, or creates one from the on-disk PNG.
    /// </summary>
    /// <param name="logoAsset">Relative asset path (e.g. <c>ligue-1/psg.png</c>), or null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Typed logo Media id, or null when <paramref name="logoAsset"/> is null/empty.</returns>
    public async Task<LogoMediaId?> GetOrImportAsync(
        string? logoAsset,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(logoAsset))
        {
            return null;
        }

        var key = Normalize(logoAsset);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var assetsRoot = Path.GetFullPath(AssetsRoot) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(AssetsRoot, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"Seed logo asset '{key}' was not found under '{AssetsRoot}'.");
        }

        await using var stream = File.OpenRead(fullPath);
        var metadata = await _media
            .CreateAsync(
                stream,
                MediaContentTypes.Png,
                stream.Length,
                Path.GetFileName(fullPath),
                cancellationToken)
            .ConfigureAwait(false);

        var id = new LogoMediaId(metadata.Id);
        return _cache.GetOrAdd(key, id);
    }

    private static string Normalize(string logoAsset) =>
        logoAsset.Replace('\\', '/').Trim().TrimStart('/');
}
