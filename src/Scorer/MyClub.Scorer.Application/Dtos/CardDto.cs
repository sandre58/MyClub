// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Application.Dtos
{
    public class CardDto : MatchEventDto
    {
        public CardColor Color { get; set; }

        public CardInfraction Infraction { get; set; }

        public string? Description { get; set; }

        public Guid? PlayerId { get; set; }
    }
}
