// -----------------------------------------------------------------------
// <copyright file="VirtualTeamType.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

/// <summary>
/// Defines the types of virtual team references based on match or fixture outcomes.
/// This enumeration is used to specify which team from a fixture result should be referenced
/// in subsequent tournament phases or rounds.
/// </summary>
public enum VirtualTeamType
{
    /// <summary>
    /// Represents the winning team from a fixture or match result.
    /// This is the most common virtual team type, used when the winner of a match
    /// should advance to the next round or phase of the competition.
    /// </summary>
    Winner,

    /// <summary>
    /// Represents the losing team from a fixture or match result.
    /// This type is used in double elimination tournaments, consolation brackets,
    /// or when losing teams get alternative competition opportunities.
    /// </summary>
    Loser
}
