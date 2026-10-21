// -----------------------------------------------------------------------
// <copyright file="DrawFeedRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Unique draw feed identity derived from a published Draw (WhoFeeds read model — not Draw ownership).
/// </summary>
public sealed record DrawFeedRef
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawFeedRef"/> class.
    /// </summary>
    /// <param name="drawId">Published draw identity that feeds the slot.</param>
    public DrawFeedRef(DrawId drawId) => DrawId = drawId;

    /// <summary>
    /// Gets the published draw identity.
    /// </summary>
    public DrawId DrawId { get; }
}
