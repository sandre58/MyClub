// -----------------------------------------------------------------------
// <copyright file="QualificationPath.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// A single routing rule from a stage ranking source to a destination slot.
/// Value object — no technical identity.
/// </summary>
public sealed record QualificationPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationPath"/> class.
    /// </summary>
    /// <param name="order">Processing / display order (≥ 1).</param>
    /// <param name="source">Where participants are taken from.</param>
    /// <param name="selection">Which participants are selected.</param>
    /// <param name="destination">Where participants are routed.</param>
    public QualificationPath(
        int order,
        QualificationSource source,
        QualificationSelection selection,
        QualificationDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(destination);

        if (order < 1)
        {
            throw new DomainException(
                "Qualification path order must be at least 1.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        Order = order;
        Source = source;
        Selection = selection;
        Destination = destination;
    }

    /// <summary>
    /// Gets the path order (≥ 1).
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the selection source.
    /// </summary>
    public QualificationSource Source { get; }

    /// <summary>
    /// Gets the selection rule.
    /// </summary>
    public QualificationSelection Selection { get; }

    /// <summary>
    /// Gets the destination.
    /// </summary>
    public QualificationDestination Destination { get; }

    /// <summary>
    /// Returns an independent copy (new nested value-object instances).
    /// </summary>
    /// <returns>A deep copy of this path.</returns>
    public QualificationPath Copy() =>
        new(
            Order,
            new QualificationSource(Source.Scope, Source.GroupId, Source.AcrossGroupsPosition),
            new QualificationSelection(Selection.Mode, Selection.Value, Selection.EndValue),
            new QualificationDestination(Destination.StageId, Destination.SlotKey));
}
