// -----------------------------------------------------------------------
// <copyright file="Card.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.Enums;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

/// <summary>
/// Represents a disciplinary card issued to a player during a football match.
/// Cards are used by referees to caution or dismiss players for various infractions,
/// and they have significant impact on match dynamics and player availability.
/// </summary>
public class Card : MatchEvent
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Card() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Card"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the card event.</param>
    /// <param name="cardColor">The color of the card issued (yellow, red, etc.).</param>
    /// <param name="playerId">The identifier of the player who received the card. Can be null for cards issued to team officials.</param>
    /// <param name="infraction">The reason or type of infraction that led to the card being issued.</param>
    /// <param name="minute">The minute of the match when the card was issued. Can be null if timing is not recorded.</param>
    private Card(MatchEventId id, CardColor cardColor, PlayerId? playerId = null, CardReason infraction = CardReason.Unknown, int? minute = null)
        : base(id, minute)
    {
        Color = cardColor;
        Infraction = infraction;
        PlayerId = playerId;
    }

    /// <summary>
    /// Creates a new card with the specified parameters.
    /// </summary>
    /// <param name="cardColor">The color of the card issued.</param>
    /// <param name="playerId">The identifier of the player who received the card. Optional.</param>
    /// <param name="infraction">The reason for the card. Defaults to Unknown if not specified.</param>
    /// <param name="minute">The minute of the match when the card was issued. Optional.</param>
    /// <returns>A new <see cref="Card"/> instance.</returns>
    public static Card Create(CardColor cardColor, PlayerId? playerId = null, CardReason infraction = CardReason.Unknown, int? minute = null) => new(MatchEventId.New(), cardColor, playerId, infraction, minute);

    /// <summary>
    /// Gets or sets the color of the card issued.
    /// This determines the severity and immediate consequences of the disciplinary action.
    /// </summary>
    public CardColor Color { get; set; }

    /// <summary>
    /// Gets or sets the reason or type of infraction that led to the card being issued.
    /// This helps categorize the disciplinary action for statistical and administrative purposes.
    /// </summary>
    public CardReason Infraction { get; set; }

    /// <summary>
    /// Gets or sets an optional description providing additional details about the incident.
    /// This can include specific circumstances or referee observations about the infraction.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the player who received the card.
    /// Can be null for cards issued to team officials (coaches, technical staff) rather than players.
    /// </summary>
    public PlayerId? PlayerId { get; set; }

    /// <summary>
    /// Returns a string representation of the card including timing, color, and player information.
    /// </summary>
    /// <returns>A formatted string describing the card event.</returns>
    public override string ToString()
    {
        var str = new StringBuilder();

        if (Minute.HasValue)
            _ = str.Append(CultureInfo.CurrentCulture, $"{Minute.Value}' : ");

        _ = str.Append(CultureInfo.CurrentCulture, $"{Color} card");

        if (PlayerId is not null)
            _ = str.Append(CultureInfo.CurrentCulture, $" ({PlayerId})");

        return str.ToString();
    }
}
