// -----------------------------------------------------------------------
// <copyright file="MatchParticipantKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// How a match participant is known to Scheduling.
/// </summary>
public enum MatchParticipantKind
{
    /// <summary>
    /// Participant identity is known (<see cref="MyClub.PlayUp.Domain.Common.EntryId"/>).
    /// </summary>
    Known = 0,

    /// <summary>
    /// Participant is structurally undetermined; dependent constraints are not evaluated.
    /// </summary>
    UnknownStructural = 1,

    /// <summary>
    /// Participant should be known but is missing from the request (InvalidRequest).
    /// </summary>
    MissingExpected = 2
}
