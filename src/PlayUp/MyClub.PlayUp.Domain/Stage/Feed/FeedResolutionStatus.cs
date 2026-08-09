// -----------------------------------------------------------------------
// <copyright file="FeedResolutionStatus.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Outcome of resolving configuration feeds for a single slot.
/// </summary>
public enum FeedResolutionStatus
{
    /// <summary>
    /// Exactly one feed definition.
    /// </summary>
    Unique = 0,

    /// <summary>
    /// No feed definition.
    /// </summary>
    Missing = 1,

    /// <summary>
    /// Two or more distinct feed kinds (at most one source each).
    /// </summary>
    MultipleFeeds = 2,

    /// <summary>
    /// Two or more sources of the same feed kind.
    /// </summary>
    InvalidFeed = 3
}
