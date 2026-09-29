// -----------------------------------------------------------------------
// <copyright file="DisciplinaryRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Extension point for competition disciplinary rules.
/// Declares which <see cref="DisciplinaryType"/> values are allowed — no consequences.
/// </summary>
public sealed record DisciplinaryRules
{
    private readonly DisciplinaryType[] _allowedTypes;

    /// <summary>
    /// Gets rules with no allowed types — no disciplinary events may be authorized by Application.
    /// </summary>
    public static DisciplinaryRules None { get; } = new([]);

    /// <summary>
    /// Initializes a new instance of the <see cref="DisciplinaryRules"/> class.
    /// </summary>
    /// <param name="allowedTypes">Allowed catalogue values (empty = none allowed; duplicates rejected; unknown enum rejected).</param>
    public DisciplinaryRules(IReadOnlyList<DisciplinaryType> allowedTypes)
    {
        ArgumentNullException.ThrowIfNull(allowedTypes);

        if (allowedTypes.Any(type => !Enum.IsDefined(type)))
        {
            throw new DomainException(
                "Allowed disciplinary types contain an unknown value.",
                RulesErrorCodes.DisciplinaryRulesInvalid);
        }

        if (allowedTypes.Distinct().Count() != allowedTypes.Count)
        {
            throw new DomainException(
                "Allowed disciplinary types cannot contain duplicates.",
                RulesErrorCodes.DisciplinaryRulesInvalid);
        }

        _allowedTypes = [..allowedTypes.OrderBy(type => (int)type)];
    }

    /// <summary>
    /// Gets the allowed disciplinary types (sorted; may be empty).
    /// </summary>
    public IReadOnlyList<DisciplinaryType> AllowedTypes => _allowedTypes;

    /// <summary>
    /// Returns whether the given type is authorized by these rules.
    /// </summary>
    public bool Allows(DisciplinaryType type) =>
        Enum.IsDefined(type) && _allowedTypes.Contains(type);

    /// <inheritdoc />
    public bool Equals(DisciplinaryRules? other) => other is not null && _allowedTypes.SequenceEqual(other._allowedTypes);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _allowedTypes.Aggregate(0, HashCode.Combine);
}
