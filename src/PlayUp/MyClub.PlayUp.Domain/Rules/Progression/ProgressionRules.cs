// -----------------------------------------------------------------------
// <copyright file="ProgressionRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// How confrontation outcomes route participants to a population or form places.
/// Authoring SoT = <see cref="Intents"/> when present; <see cref="Paths"/> are the atomic Apply model
/// (derived via <see cref="ProgressionPathExpander"/>, or path-list authoring).
/// </summary>
public sealed record ProgressionRules
{
    private readonly ProgressionIntent[] _intents;
    private readonly ProgressionPath[] _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionRules"/> class from atomic paths.
    /// </summary>
    /// <param name="paths">Progression paths (non-empty; unique slot destinations; population may be shared).</param>
    public ProgressionRules(IReadOnlyList<ProgressionPath> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _intents = [];
        _paths = NormalizePaths(paths);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionRules"/> class from intents and Expand projection.
    /// </summary>
    private ProgressionRules(
        IReadOnlyList<ProgressionIntent> intents,
        IReadOnlyList<ProgressionPath> paths)
    {
        _intents = [.. intents.OrderBy(i => i.Order)];
        _paths = NormalizePaths(paths);
    }

    /// <summary>
    /// Hydrates from persistence (intents SoT + path projection written at last commit).
    /// </summary>
    public static ProgressionRules FromPersisted(
        IReadOnlyList<ProgressionIntent> intents,
        IReadOnlyList<ProgressionPath> paths)
    {
        ArgumentNullException.ThrowIfNull(intents);
        ArgumentNullException.ThrowIfNull(paths);

        return intents.Count == 0
            ? new ProgressionRules(paths)
            : paths.Count == 0
            ? throw new DomainException(
                "Persisted progression intents require a path projection.",
                RulesErrorCodes.ProgressionRulesInvalid)
            : intents.Select(i => i.Order).Distinct().Count() != intents.Count
            ? throw new DomainException(
                "Progression intent orders must be unique.",
                RulesErrorCodes.ProgressionRulesInvalid)
            : new ProgressionRules(intents, paths);
    }

    /// <summary>
    /// Builds rules from authoring intents by Expand (paths derived at commit).
    /// </summary>
    public static ProgressionRules FromIntents(
        IReadOnlyList<ProgressionIntent> intents,
        IReadOnlyList<Round> rounds)
    {
        ArgumentNullException.ThrowIfNull(intents);
        var paths = ProgressionPathExpander.Materialize(intents, rounds);
        return new ProgressionRules(intents, paths);
    }

    /// <summary>Gets authoring intents (empty for path-list-only rules).</summary>
    public IReadOnlyList<ProgressionIntent> Intents => _intents;

    /// <summary>Gets atomic paths for Apply.</summary>
    public IReadOnlyList<ProgressionPath> Paths => _paths;

    /// <summary>Returns an independent copy.</summary>
    public ProgressionRules Copy() =>
        _intents.Length > 0
            ? new ProgressionRules(
                [.. _intents.Select(i => i.Copy())],
                [.. _paths.Select(p => p.Copy())])
            : new ProgressionRules([.. _paths.Select(p => p.Copy())]);

    /// <inheritdoc />
    public bool Equals(ProgressionRules? other) =>
        other is not null && _paths.SequenceEqual(other._paths);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _paths.Aggregate(0, HashCode.Combine);

    private static ProgressionPath[] NormalizePaths(IReadOnlyList<ProgressionPath> paths)
    {
        if (paths.Count == 0)
        {
            throw new DomainException(
                "Progression rules require at least one path.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        var sources = paths
            .Select(p => (p.SourceFixtureId, p.Outcome))
            .ToArray();
        if (sources.Distinct().Count() != sources.Length)
        {
            throw new DomainException(
                "Progression paths must have unique source fixture and outcome pairs.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        var slotDestinationKeys = paths
            .Where(p => p.Destination.TargetsSlot)
            .Select(p => (p.Destination.StageId, p.Destination.SlotKey!))
            .ToArray();
        return slotDestinationKeys.Distinct().Count() != slotDestinationKeys.Length
            ? throw new DomainException(
                "Progression paths must target unique slot destinations.",
                RulesErrorCodes.ProgressionRulesInvalid)
            : [
            ..paths.OrderBy(p => p.SourceFixtureId.Value)
                .ThenBy(p => p.Outcome)
                .ThenBy(p => p.Destination.StageId.Value)
                .ThenBy(p => p.Destination.SlotKey ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(p => p.Destination.GroupId?.Value ?? Guid.Empty)
        ];
    }
}
