// -----------------------------------------------------------------------
// <copyright file="StructureMatchProductionMessageCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Stable codes for match production presentation copy (SPA maps to structure i18n).
/// Disabled materialize reasons reuse <see cref="StructureMaterializeDisabledReasons"/> verbatim.
/// </summary>
public static class StructureMatchProductionMessageCodes
{
    public const string Unavailable = "Unavailable";
    public const string StatusUnknown = "StatusUnknown";
    public const string Complete = "Complete";
    public const string Partial = "Partial";
    public const string NoDeterminedConfrontations = "NoDeterminedConfrontations";
    public const string DeterminedPartial = "DeterminedPartial";
    public const string DeterminedComplete = "DeterminedComplete";
    public const string MatchesBlockedByGroupsPools = "MatchesBlockedByGroupsPools";
    public const string FollowUpExhausted = "FollowUpExhausted";
    public const string FrozenPartial = "FrozenPartial";

    public const string PrerequisiteComposition = "PrerequisiteComposition";
    public const string PrerequisiteGroupsPlacement = "PrerequisiteGroupsPlacement";
    public const string PrerequisiteCupEntryRound = "PrerequisiteCupEntryRound";
}
