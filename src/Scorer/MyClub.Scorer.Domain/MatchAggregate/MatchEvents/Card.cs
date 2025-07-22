// -----------------------------------------------------------------------
// <copyright file="Card.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.Enums;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

public class Card : MatchEvent
{
    // <remarks>Used by EF Core</remarks>
    private Card()
        : base() { }

    private Card(MatchEventId id, CardColor cardColor, PlayerId? playerId = null, CardReason infraction = CardReason.Unknown, int? minute = null)
        : base(id, minute)
    {
        Color = cardColor;
        Infraction = infraction;
        PlayerId = playerId;
    }

    public static Card Create(CardColor cardColor, PlayerId? playerId = null, CardReason infraction = CardReason.Unknown, int? minute = null) => new(MatchEventId.New(), cardColor, playerId, infraction, minute);

    public CardColor Color { get; set; }

    public CardReason Infraction { get; set; }

    public string? Description { get; set; }

    public PlayerId? PlayerId { get; set; }

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
