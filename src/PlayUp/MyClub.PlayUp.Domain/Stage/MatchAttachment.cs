// -----------------------------------------------------------------------
// <copyright file="MatchAttachment.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Link from a fixture to an attached match with an explicit confrontation leg index.
/// </summary>
/// <remarks>
/// <see cref="LegIndex"/> is 1-based (1 = first leg, 2 = second leg). Collection order of attachments is not a business semantic.
/// </remarks>
public sealed record MatchAttachment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchAttachment"/> class.
    /// </summary>
    /// <param name="matchId">Attached match identity.</param>
    /// <param name="legIndex">1-based leg index within the fixture.</param>
    public MatchAttachment(MatchId matchId, int legIndex)
    {
        if (legIndex < 1)
        {
            throw new DomainException(
                "Fixture match attachment leg index must be at least 1.",
                StageErrorCodes.InvalidConfiguration);
        }

        MatchId = matchId;
        LegIndex = legIndex;
    }

    /// <summary>
    /// Gets the attached match identity.
    /// </summary>
    public MatchId MatchId { get; }

    /// <summary>
    /// Gets the 1-based leg index within the fixture.
    /// </summary>
    public int LegIndex { get; }
}
