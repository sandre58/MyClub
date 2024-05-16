// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.DatabaseContext.Domain.PlayerAggregate
{
    public class RatedPosition : Entity
    {
        public RatedPosition() { }

        public RatedPosition(Guid id) : base(id) { }

        public string? Position { get; set; }

        public string? Rating { get; set; }

        public bool IsNatural { get; set; }

        public Player? Player { get; set; }

        public override string ToString() => $"{Position} | {Rating}";

        public override bool IsSimilar(object? obj) => obj is RatedPosition other && Position == other.Position && Player == other.Player;

        public override void SetFrom(object? from)
        {
            if (from is RatedPosition ratedPosition)
            {
                Rating = ratedPosition.Rating;
                IsNatural = ratedPosition.IsNatural;
            }
        }
    }
}
