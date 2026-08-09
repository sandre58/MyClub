// -----------------------------------------------------------------------
// <copyright file="StageErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Stable machine-readable codes for Stage aggregate invariant violations.
/// </summary>
public static class StageErrorCodes
{
    /// <summary>
    /// Gets the code for an invalid lifecycle or capability transition.
    /// </summary>
    public const string InvalidTransition = "Stage.InvalidTransition";

    /// <summary>
    /// Gets the code when the stage is not ready to prepare or start.
    /// </summary>
    public const string NotReady = "Stage.NotReady";

    /// <summary>
    /// Gets the code when the stage configuration is invalid for Prepare.
    /// </summary>
    public const string InvalidConfiguration = "Stage.InvalidConfiguration";

    /// <summary>
    /// Gets the code when an illegal composition is requested.
    /// </summary>
    public const string InvalidComposition = "Stage.InvalidComposition";

    /// <summary>
    /// Gets the code when structure mutation is attempted after Running.
    /// </summary>
    public const string StructureLocked = "Stage.StructureLocked";

    /// <summary>
    /// Gets the code when a group cannot be found.
    /// </summary>
    public const string GroupNotFound = "Stage.GroupNotFound";

    /// <summary>
    /// Gets the code when a round cannot be found.
    /// </summary>
    public const string RoundNotFound = "Stage.RoundNotFound";

    /// <summary>
    /// Gets the code when a matchday cannot be found.
    /// </summary>
    public const string MatchdayNotFound = "Stage.MatchdayNotFound";

    /// <summary>
    /// Gets the code when an entry is already assigned to a group.
    /// </summary>
    public const string DuplicateEntry = "Stage.DuplicateEntry";

    /// <summary>
    /// Gets the code when an entry is not present in the target group.
    /// </summary>
    public const string EntryNotFound = "Stage.EntryNotFound";

    /// <summary>
    /// Gets the code when Arrange* is not a permutation of current ids.
    /// </summary>
    public const string InvalidOrder = "Stage.InvalidOrder";

    /// <summary>
    /// Gets the code when a stage name is invalid.
    /// </summary>
    public const string NameInvalid = "Stage.NameInvalid";

    /// <summary>
    /// Gets the code when a group or round name is invalid.
    /// </summary>
    public const string InvalidDisplayName = "Stage.InvalidDisplayName";

    /// <summary>
    /// Gets the code when a fixture cannot be found.
    /// </summary>
    public const string FixtureNotFound = "Stage.FixtureNotFound";

    /// <summary>
    /// Gets the code when a match identity is already attached to a fixture in the stage.
    /// </summary>
    public const string MatchAlreadyAttached = "Stage.MatchAlreadyAttached";

    /// <summary>
    /// Gets the code when a match identity is not attached to the target fixture.
    /// </summary>
    public const string MatchNotAttached = "Stage.MatchNotAttached";

    /// <summary>
    /// Gets the code when a slot key is empty or too long.
    /// </summary>
    public const string SlotKeyInvalid = "Stage.SlotKeyInvalid";

    /// <summary>
    /// Gets the code when a slot key already exists in the stage.
    /// </summary>
    public const string DuplicateSlotKey = "Stage.DuplicateSlotKey";

    /// <summary>
    /// Gets the code when a slot cannot be found.
    /// </summary>
    public const string SlotNotFound = "Stage.SlotNotFound";

    /// <summary>
    /// Gets the code when a slot cannot be removed because it is still referenced.
    /// </summary>
    public const string SlotReferenced = "Stage.SlotReferenced";

    /// <summary>
    /// Gets the code when a direct assignment conflicts with a declarative feed.
    /// </summary>
    public const string SlotFeedConflict = "Stage.SlotFeedConflict";

    /// <summary>
    /// Gets the code when a slot has more than one local feed (Direct and/or Progression).
    /// </summary>
    public const string MultipleFeeds = "Stage.MultipleFeeds";
}
