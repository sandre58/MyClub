// -----------------------------------------------------------------------
// <copyright file="CardReason.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Domain.Enums;

/// <summary>
/// Enumeration representing the reasons why a card was issued during a match.
/// This categorizes the type of infraction or behavior that led to the card being shown.
/// </summary>
public enum CardReason
{
    /// <summary>
    /// The reason for the card is unknown or not specified.
    /// This is used when the specific infraction details are not available.
    /// </summary>
    Unknown,

    /// <summary>
    /// Card issued for unsportsmanlike behavior.
    /// This includes actions that go against the spirit of fair play, such as
    /// arguing with officials, disrespectful conduct, or poor sportsmanship.
    /// </summary>
    UnsportsmanlikeBehavior,

    /// <summary>
    /// Card issued for protests against referee decisions.
    /// This includes dissent, arguing with match officials, or challenging their authority.
    /// </summary>
    Protests
}
