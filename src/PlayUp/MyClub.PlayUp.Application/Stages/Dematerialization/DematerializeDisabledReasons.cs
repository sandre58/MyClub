// -----------------------------------------------------------------------
// <copyright file="DematerializeDisabledReasons.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Stable Structure workspace codes when dematerialization is exposed but disabled.
/// </summary>
public static class DematerializeDisabledReasons
{
    /// <summary>At least one in-scope match is Live, Finished, or Cancelled after Live.</summary>
    public const string MatchLifecycleStarted = "MatchLifecycleStarted";

    /// <summary>At least one in-scope match is currently Live (pause phase).</summary>
    public const string MatchLiveInProgress = "MatchLiveInProgress";

    /// <summary>Topology and persisted matches are inconsistent (orphan, Swiss gaps, etc.).</summary>
    public const string MaterializationStateInconsistent = "MaterializationStateInconsistent";

    /// <summary>Competition or stage lifecycle does not allow dematerialization.</summary>
    public const string StageLifecycleNotSupported = "StageLifecycleNotSupported";

    /// <summary>Format is not supported for dematerialization on this surface.</summary>
    public const string FormatNotSupported = "FormatNotSupported";
}
