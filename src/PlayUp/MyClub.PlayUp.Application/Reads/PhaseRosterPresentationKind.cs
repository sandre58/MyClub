// -----------------------------------------------------------------------
// <copyright file="PhaseRosterPresentationKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Presentation classifier for downstream phase roster gaps (read model only — does not change Start gates).
/// </summary>
public enum PhaseRosterPresentationKind
{
    Complete,
    AwaitingInboundQualification,
    CompositionIncomplete,
}
