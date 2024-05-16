// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities.Extensions;
using MyClub.Domain;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.PersonAggregate
{
    public class RatedPosition : Entity
    {
        public RatedPosition(Position position, PositionRating positionRating = PositionRating.Natural, Guid? id = null) : base(id)
            => (Position, Rating) = (position.IsRequiredOrThrow(nameof(Position)), positionRating);

        public Position Position { get; }

        public PositionRating Rating { get; set; }

        public bool IsNatural { get; set; }

        public override string ToString() => $"{Position} | {Rating}";
    }
}
