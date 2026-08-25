// -----------------------------------------------------------------------
// <copyright file="SeedSpec.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Parsed seed target: catalog id + optional progress.
/// </summary>
/// <param name="Id">Catalog id (after alias resolution).</param>
/// <param name="Progress">Explicit progress when provided; otherwise null (default Running for progressive seeds).</param>
public readonly record struct SeedSpec(string Id, SeedProgress? Progress)
{
    /// <summary>Default progress when omitted on progressive seeds.</summary>
    public const SeedProgress DefaultProgress = SeedProgress.Running;

    /// <summary>Gets the progress to apply for progressive seeds.</summary>
    public SeedProgress EffectiveProgress => Progress ?? DefaultProgress;

    /// <summary>
    /// Parses <c>id</c> or <c>id:progress</c>, applying short compatibility aliases.
    /// </summary>
    /// <param name="raw">Raw token.</param>
    /// <returns>Normalized seed spec.</returns>
    public static SeedSpec Parse(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        var token = raw.Trim();

        if (Aliases.TryGetValue(token, out var aliased))
        {
            return aliased;
        }

        var separator = token.LastIndexOf(':');
        if (separator <= 0)
        {
            return new SeedSpec(token, Progress: null);
        }

        var id = token[..separator].Trim();
        var progressToken = token[(separator + 1)..].Trim();
        if (!TryParseProgress(progressToken, out var progress))
        {
            throw new InvalidOperationException(
                $"Unknown progress '{progressToken}'. Expected prepared, running, or finished.");
        }

        if (Aliases.TryGetValue(id, out var baseAlias))
        {
            return new SeedSpec(baseAlias.Id, progress);
        }

        return new SeedSpec(id, progress);
    }

    /// <summary>
    /// Parses a progress token.
    /// </summary>
    /// <param name="token">Progress token.</param>
    /// <param name="progress">Parsed value.</param>
    /// <returns>True when recognized.</returns>
    public static bool TryParseProgress(string token, out SeedProgress progress)
    {
        progress = DefaultProgress;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        switch (token.Trim().ToUpperInvariant())
        {
            case "PREPARED":
            case "PREPARE":
                progress = SeedProgress.Prepared;
                return true;
            case "RUNNING":
            case "MID":
            case "IN-PROGRESS":
                progress = SeedProgress.Running;
                return true;
            case "FINISHED":
            case "COMPLETED":
            case "COMPLETE":
                progress = SeedProgress.Finished;
                return true;
            default:
                return false;
        }
    }

    private static readonly Dictionary<string, SeedSpec> Aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["group-stage-mid"] = new SeedSpec("groups", SeedProgress.Running),
            ["knockout-qf"] = new SeedSpec("cup", SeedProgress.Running),
            ["finished"] = new SeedSpec("groups", SeedProgress.Finished),
        };
}
