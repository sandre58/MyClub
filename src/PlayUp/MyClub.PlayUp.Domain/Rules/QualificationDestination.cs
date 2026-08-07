// -----------------------------------------------------------------------
// <copyright file="QualificationDestination.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Where selected participants are routed (destination stage + opaque slot key).
/// Does not express home/away.
/// </summary>
public sealed record QualificationDestination
{
    /// <summary>
    /// Maximum allowed length of a slot key after trim.
    /// </summary>
    public const int SlotKeyMaxLength = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationDestination"/> class.
    /// </summary>
    /// <param name="stageId">The destination stage identity.</param>
    /// <param name="slotKey">Opaque business slot key belonging to the destination stage.</param>
    public QualificationDestination(StageId stageId, string slotKey)
    {
        ArgumentNullException.ThrowIfNull(slotKey);

        var trimmed = slotKey.Trim();
        SlotKey = trimmed.Length switch
        {
            0 => throw new DomainException(
                "Slot key cannot be empty.",
                RulesErrorCodes.QualificationRulesInvalid),
            > SlotKeyMaxLength => throw new DomainException(
                $"Slot key cannot exceed {SlotKeyMaxLength} characters.",
                RulesErrorCodes.QualificationRulesInvalid),
            _ => trimmed
        };

        StageId = stageId;
    }

    /// <summary>
    /// Gets the destination stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the opaque destination slot key (e.g. QuarterFinal1, Consolante1).
    /// </summary>
    public string SlotKey { get; }
}
