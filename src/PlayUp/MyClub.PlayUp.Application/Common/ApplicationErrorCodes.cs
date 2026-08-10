// -----------------------------------------------------------------------
// <copyright file="ApplicationErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application;

/// <summary>
/// Stable machine-readable codes for application orchestration failures.
/// </summary>
public static class ApplicationErrorCodes
{
    /// <summary>
    /// Gets the code when an inbound feed targets a missing slot on the destination stage.
    /// </summary>
    public const string DanglingFeedTarget = "Application.DanglingFeedTarget";

    /// <summary>
    /// Gets the code when the target stage is not in the provided competition stages list.
    /// </summary>
    public const string StageNotInCompetition = "Application.StageNotInCompetition";

    /// <summary>
    /// Gets the code when WhoFeeds validation fails before Prepare.
    /// </summary>
    public const string SlotFeedsInvalid = "Application.SlotFeedsInvalid";

    /// <summary>
    /// Gets the code when Fixture↔Match coherence fails for V1 progression (e.g. not exactly one match).
    /// </summary>
    public const string FixtureInvalid = "Application.FixtureInvalid";
}
