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

    /// <summary>
    /// Legacy code: group-scoped paths once rejected before multi-Standing orchestration (7.0.9.2).
    /// Prefer <see cref="QualificationStandingMissing"/> / <see cref="QualificationGroupNotFound"/>.
    /// </summary>
    public const string QualificationSourceNotSupported = "Application.QualificationSourceNotSupported";

    /// <summary>
    /// Gets the code when a qualification path needs a standing that was not supplied.
    /// </summary>
    public const string QualificationStandingMissing = "Application.QualificationStandingMissing";

    /// <summary>
    /// Gets the code when a qualification path references a group absent from the source stage.
    /// </summary>
    public const string QualificationGroupNotFound = "Application.QualificationGroupNotFound";

    /// <summary>
    /// Gets the code when a published Draw resolution cannot be applied to the current Stage state.
    /// Distinct from Domain <c>NoSolution</c> (solver found no resolution).
    /// </summary>
    public const string DrawApplyFailure = "Application.DrawApplyFailure";

    /// <summary>
    /// Gets the code when ApplyDraw does not support the Draw resolution kind (e.g. Pairing in V1.A).
    /// </summary>
    public const string DrawKindNotSupported = "Application.DrawKindNotSupported";
}
