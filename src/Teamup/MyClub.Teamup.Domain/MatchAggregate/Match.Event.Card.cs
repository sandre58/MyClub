// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.MatchAggregate
{
    public class Card : MatchEvent
    {
        public Card(CardColor cardColor, Player? player = null, CardInfraction infraction = CardInfraction.Unknown, int? minute = null, Guid? id = null) : base(minute, id)
        {
            Player = player;
            Color = cardColor;
            Infraction = infraction;
        }

        public CardColor Color { get; set; }

        public CardInfraction Infraction { get; set; }

        public string? Description { get; set; }

        public Player? Player { get; set; }

        public override string ToString()
        {
            var str = new StringBuilder();

            if (Minute.HasValue)
                str.Append($"{Minute.Value}' : ");

            str.Append($"{Color} card");

            if (Player is not null)
                str.Append($" ({Player})");

            return str.ToString();
        }
    }
}
