// -----------------------------------------------------------------------
// <copyright file="Matchday.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// A matchday (journée) within a stage. Placement at Stage level is provisional until Match.
/// </summary>
[DebuggerDisplay("Matchday {Number}")]
public sealed class Matchday : Entity<MatchdayId>
{
    internal Matchday(MatchdayId id, int number)
        : base(id)
    {
        if (number < 1)
        {
            throw new DomainException(
                "Matchday number must be greater than or equal to 1.",
                StageErrorCodes.InvalidConfiguration);
        }

        Number = number;
    }

    /// <summary>
    /// Gets the matchday number (1-based).
    /// </summary>
    public int Number { get; }
}
