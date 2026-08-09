// -----------------------------------------------------------------------
// <copyright file="ProgressionDestination.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Where a progression outcome is routed (destination stage + opaque slot key).
/// Does not express home/away or bracket side.
/// </summary>
public sealed record ProgressionDestination
{
    /// <summary>
    /// Maximum allowed length of a slot key after trim (aligned with qualification destinations).
    /// </summary>
    public const int SlotKeyMaxLength = QualificationDestination.SlotKeyMaxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionDestination"/> class.
    /// </summary>
    /// <param name="stageId">The destination stage identity.</param>
    /// <param name="slotKey">Opaque business slot key belonging to the destination stage.</param>
    public ProgressionDestination(StageId stageId, string slotKey)
    {
        ArgumentNullException.ThrowIfNull(slotKey);

        var trimmed = slotKey.Trim();
        SlotKey = trimmed.Length switch
        {
            0 => throw new DomainException(
                "Slot key cannot be empty.",
                RulesErrorCodes.ProgressionRulesInvalid),
            > SlotKeyMaxLength => throw new DomainException(
                $"Slot key cannot exceed {SlotKeyMaxLength} characters.",
                RulesErrorCodes.ProgressionRulesInvalid),
            _ => trimmed
        };

        StageId = stageId;
    }

    /// <summary>
    /// Gets the destination stage identity.
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the opaque destination slot key.
    /// </summary>
    public string SlotKey { get; }
}
