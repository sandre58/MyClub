// -----------------------------------------------------------------------
// <copyright file="QualificationSlotOverride.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Custom mapping entry: source occurrence → destination slot key.
/// </summary>
/// <param name="Occurrence">Expanded source occurrence.</param>
/// <param name="SlotKey">Destination slot key on the intent destination stage.</param>
public sealed record QualificationSlotOverride(
    QualificationSourceOccurrence Occurrence,
    string SlotKey);
