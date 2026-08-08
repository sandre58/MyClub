// -----------------------------------------------------------------------
// <copyright file="DrawConstraint.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// A single draw pairing constraint (type + enforcement). No execution logic.
/// </summary>
public sealed record DrawConstraint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawConstraint"/> class.
    /// </summary>
    /// <param name="type">The constraint kind.</param>
    /// <param name="enforcement">How strictly the constraint applies; defaults to <see cref="ConstraintEnforcement.Preferred"/>.</param>
    public DrawConstraint(DrawConstraintType type, ConstraintEnforcement enforcement = ConstraintEnforcement.Preferred)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                "Draw constraint type is unknown.",
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
    }

    /// <summary>
    /// Gets the constraint kind.
    /// </summary>
    public DrawConstraintType ConstraintType { get; }

    /// <summary>
    /// Gets how strictly the constraint must be respected.
    /// </summary>
    public ConstraintEnforcement Enforcement { get; }
}
