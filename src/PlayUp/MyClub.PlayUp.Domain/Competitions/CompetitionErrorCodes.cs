// -----------------------------------------------------------------------
// <copyright file="CompetitionErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Stable machine-readable codes for Competition aggregate invariant violations.
/// </summary>
public static class CompetitionErrorCodes
{
    /// <summary>
    /// Gets the code for an invalid lifecycle or capability transition.
    /// </summary>
    public const string InvalidTransition = "Competition.InvalidTransition";

    /// <summary>
    /// Gets the code when a TeamId already has an occupying entry.
    /// </summary>
    public const string DuplicateTeam = "Competition.DuplicateTeam";

    /// <summary>
    /// Gets the code when a StageId is already attached.
    /// </summary>
    public const string DuplicateStage = "Competition.DuplicateStage";

    /// <summary>
    /// Gets the code when an entry cannot be found.
    /// </summary>
    public const string EntryNotFound = "Competition.EntryNotFound";

    /// <summary>
    /// Gets the code when a stage reference cannot be found.
    /// </summary>
    public const string StageNotFound = "Competition.StageNotFound";

    /// <summary>
    /// Gets the code when SetStageOrder is not a permutation of current stage ids.
    /// </summary>
    public const string InvalidStageOrder = "Competition.InvalidStageOrder";

    /// <summary>
    /// Gets the code when a display name is invalid.
    /// </summary>
    public const string InvalidDisplayName = "Competition.InvalidDisplayName";

    /// <summary>
    /// Gets the code when a competition name is invalid.
    /// </summary>
    public const string NameInvalid = "Competition.NameInvalid";
}
