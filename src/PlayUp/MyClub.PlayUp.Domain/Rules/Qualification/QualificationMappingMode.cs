// -----------------------------------------------------------------------
// <copyright file="QualificationMappingMode.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// How destination slots are assigned to expanded source occurrences.
/// </summary>
public enum QualificationMappingMode
{
    /// <summary>Zip Expand order with destination SlotOrder.</summary>
    Canonical = 0,

    /// <summary>Canonical zip then apply SlotOverrides keyed by source occurrence.</summary>
    Custom = 1
}
