// -----------------------------------------------------------------------
// <copyright file="StructureMaterializeDisabledReasons.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Minimal causal codes for Structure workspace materialize when the command is disabled (not terminal).
/// </summary>
public static class StructureMaterializeDisabledReasons
{
    public const string StructureNotMutable = "StructureNotMutable";
    public const string NoDeterminedConfrontations = "NoDeterminedConfrontations";
    public const string GroupsNotReady = "GroupsNotReady";
    public const string NoEligibleConfrontations = "NoEligibleConfrontations";
}
