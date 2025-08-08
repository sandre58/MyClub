// -----------------------------------------------------------------------
// <copyright file="CardColor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the different colors of cards that can be shown during a match.
/// Each color has specific meanings and consequences in sports officiating.
/// </summary>
public enum CardColor
{
    /// <summary>
    /// Yellow card - typically a caution or warning for minor infractions.
    /// Used to warn players about their behavior or for minor rule violations.
    /// </summary>
    Yellow,

    /// <summary>
    /// Red card - ejection from the game for serious infractions.
    /// Results in the player being sent off and unable to return to the current match.
    /// </summary>
    Red,

    /// <summary>
    /// White card - used in some sports for fair play recognition.
    /// Typically awarded for exemplary sportsmanship or fair play behavior.
    /// </summary>
    White,

    /// <summary>
    /// Black card - used in some sports for temporary suspension.
    /// May result in a temporary removal from play or specific penalties.
    /// </summary>
    Black,

    /// <summary>
    /// Green card - used in some sports for positive recognition.
    /// Often awarded for good sportsmanship or adherence to fair play principles.
    /// </summary>
    Green
}
