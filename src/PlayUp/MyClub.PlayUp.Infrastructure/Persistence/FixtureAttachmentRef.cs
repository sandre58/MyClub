// -----------------------------------------------------------------------
// <copyright file="FixtureAttachmentRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Infrastructure row for fixture match attachments (Domain Fixture.Attachments).
/// </summary>
internal sealed class FixtureAttachmentRef
{
    /// <summary>
    /// Gets or sets the owning fixture identity.
    /// </summary>
    public FixtureId FixtureId { get; set; }

    /// <summary>
    /// Gets or sets the 1-based leg index.
    /// </summary>
    public int LegIndex { get; set; }

    /// <summary>
    /// Gets or sets the attached match identity.
    /// </summary>
    public MatchId MatchId { get; set; }
}
