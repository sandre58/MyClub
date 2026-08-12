// -----------------------------------------------------------------------
// <copyright file="DrawConstraint.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// A single draw constraint (type + enforcement + optional typed parameter). No execution logic.
/// </summary>
public sealed record DrawConstraint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawConstraint"/> class for non-parameterized types.
    /// </summary>
    /// <param name="type">The constraint kind (not <see cref="DrawConstraintType.MaxSameAssociationPerGroup"/>).</param>
    /// <param name="enforcement">How strictly the constraint applies; defaults to <see cref="ConstraintEnforcement.Preferred"/>.</param>
    public DrawConstraint(DrawConstraintType type, ConstraintEnforcement enforcement = ConstraintEnforcement.Preferred)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                "Draw constraint type is unknown.",
                RulesErrorCodes.DrawRulesInvalid);
        }

        if (type == DrawConstraintType.MaxSameAssociationPerGroup)
        {
            throw new DomainException(
                "MaxSameAssociationPerGroup requires MaxPerGroup; use MaxSameAssociationPerGroup(...).",
                RulesErrorCodes.DrawRulesInvalid);
        }

        if (!Enum.IsDefined(enforcement))
        {
            throw new DomainException(
                "Draw constraint enforcement is unknown.",
                RulesErrorCodes.DrawRulesInvalid);
        }

        ConstraintType = type;
        Enforcement = enforcement;
        MaxPerGroup = null;
    }

    private DrawConstraint(int maxPerGroup, ConstraintEnforcement enforcement)
    {
        if (maxPerGroup <= 0)
        {
            throw new DomainException(
                "MaxPerGroup must be greater than zero.",
                RulesErrorCodes.DrawRulesInvalid);
        }

        if (!Enum.IsDefined(enforcement))
        {
            throw new DomainException(
                "Draw constraint enforcement is unknown.",
                RulesErrorCodes.DrawRulesInvalid);
        }

        if (enforcement != ConstraintEnforcement.Required)
        {
            throw new DomainException(
                "MaxSameAssociationPerGroup supports Required enforcement only in V1.",
                RulesErrorCodes.DrawRulesInvalid);
        }

        ConstraintType = DrawConstraintType.MaxSameAssociationPerGroup;
        Enforcement = enforcement;
        MaxPerGroup = maxPerGroup;
    }

    /// <summary>
    /// Creates a Required Group composition constraint: at most <paramref name="maxPerGroup"/>
    /// entries sharing an association in the same group.
    /// </summary>
    /// <param name="maxPerGroup">Maximum entries per association per group (&gt; 0).</param>
    /// <param name="enforcement">Must be <see cref="ConstraintEnforcement.Required"/> (V1).</param>
    /// <returns>A parameterized draw constraint.</returns>
    public static DrawConstraint MaxSameAssociationPerGroup(
        int maxPerGroup,
        ConstraintEnforcement enforcement = ConstraintEnforcement.Required) =>
        new(maxPerGroup, enforcement);

    /// <summary>
    /// Gets the constraint kind.
    /// </summary>
    public DrawConstraintType ConstraintType { get; }

    /// <summary>
    /// Gets how strictly the constraint must be respected.
    /// </summary>
    public ConstraintEnforcement Enforcement { get; }

    /// <summary>
    /// Gets MaxPerGroup when <see cref="ConstraintType"/> is
    /// <see cref="DrawConstraintType.MaxSameAssociationPerGroup"/>; otherwise <see langword="null"/>.
    /// </summary>
    public int? MaxPerGroup { get; }

    /// <summary>
    /// Returns an independent copy.
    /// </summary>
    /// <returns>A copy of this constraint.</returns>
    public DrawConstraint Copy() =>
        ConstraintType == DrawConstraintType.MaxSameAssociationPerGroup
            ? MaxSameAssociationPerGroup(MaxPerGroup!.Value, Enforcement)
            : new DrawConstraint(ConstraintType, Enforcement);
}
