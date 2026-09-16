// -----------------------------------------------------------------------
// <copyright file="QualificationRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// How participants leave a stage via ranking → slots.
/// Authoring SoT = <see cref="Intents"/>; <see cref="Paths"/> are the atomic Apply model
/// (derived via <see cref="QualificationPathExpander"/>, or legacy path list).
/// </summary>
public sealed record QualificationRules
{
    private readonly QualificationIntent[] _intents;
    private readonly QualificationPath[] _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationRules"/> class.
    /// Legacy constructor: ordered atomic paths (migrates Position paths to singleton intents when possible).
    /// </summary>
    /// <param name="paths">Ordered qualification paths (non-empty, unique orders).</param>
    public QualificationRules(IReadOnlyList<QualificationPath> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (paths.Count == 0)
        {
            throw new DomainException(
                "Qualification rules require at least one path.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (paths.Select(p => p.Order).Distinct().Count() != paths.Count)
        {
            throw new DomainException(
                "Qualification path orders must be unique.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        _paths = [.. paths.OrderBy(p => p.Order)];
        _intents = TryMigrateSingletons(_paths);
    }

    private QualificationRules(
        IReadOnlyList<QualificationIntent> intents,
        IReadOnlyList<QualificationPath> paths)
    {
        _intents = [.. intents.OrderBy(i => i.Order)];
        _paths = [.. paths.OrderBy(p => p.Order)];
    }

    /// <summary>
    /// Hydrates from persistence (intents SoT + path projection written at last commit).
    /// </summary>
    public static QualificationRules FromPersisted(
        IReadOnlyList<QualificationIntent> intents,
        IReadOnlyList<QualificationPath> paths)
    {
        ArgumentNullException.ThrowIfNull(intents);
        ArgumentNullException.ThrowIfNull(paths);

        return intents.Count == 0
            ? new QualificationRules(paths)
            : paths.Count == 0
            ? throw new DomainException(
                "Persisted qualification intents require a path projection.",
                RulesErrorCodes.QualificationRulesInvalid)
            : intents.Select(i => i.Order).Distinct().Count() != intents.Count
            ? throw new DomainException(
                "Qualification intent orders must be unique.",
                RulesErrorCodes.QualificationRulesInvalid)
            : paths.Select(p => p.Order).Distinct().Count() != paths.Count
            ? throw new DomainException(
                "Qualification path orders must be unique.",
                RulesErrorCodes.QualificationRulesInvalid)
            : new QualificationRules(intents, paths);
    }

    /// <summary>
    /// Builds rules from authoring intents by Expand + Map (paths derived at commit).
    /// </summary>
    /// <param name="intents">Authoring intents.</param>
    /// <param name="groupOrder">Canonical group order of the source stage.</param>
    /// <param name="slotOrderByStage">Canonical slot keys per destination stage.</param>
    /// <returns>Rules with intents + materialized paths.</returns>
    public static QualificationRules FromIntents(
        IReadOnlyList<QualificationIntent> intents,
        IReadOnlyList<GroupId> groupOrder,
        IReadOnlyDictionary<StageId, IReadOnlyList<string>> slotOrderByStage)
    {
        ArgumentNullException.ThrowIfNull(intents);
        var paths = QualificationPathExpander.Materialize(intents, groupOrder, slotOrderByStage);
        return new QualificationRules(intents, paths);
    }

    /// <summary>
    /// Gets authoring intents (empty for legacy non-Position path sets).
    /// </summary>
    public IReadOnlyList<QualificationIntent> Intents => _intents;

    /// <summary>
    /// Gets atomic paths for Apply / WhoFeeds (derived when intents are the SoT).
    /// </summary>
    public IReadOnlyList<QualificationPath> Paths => _paths;

    /// <summary>
    /// Returns an independent copy.
    /// </summary>
    public QualificationRules Copy() =>
        _intents.Length > 0
            ? new QualificationRules(
                [.. _intents.Select(i => i.Copy())],
                [.. _paths.Select(p => p.Copy())])
            : new QualificationRules([.. _paths.Select(p => p.Copy())]);

    /// <inheritdoc />
    public bool Equals(QualificationRules? other) =>
        other is not null && _paths.SequenceEqual(other._paths);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _paths.Aggregate(0, HashCode.Combine);

    private static QualificationIntent[] TryMigrateSingletons(QualificationPath[] paths) =>
        paths.Any(p => p.Selection.Mode != SelectionMode.Position)
            ? []
            : [.. paths.Select(p => QualificationPathExpander.ToSingletonIntent(p))];
}
