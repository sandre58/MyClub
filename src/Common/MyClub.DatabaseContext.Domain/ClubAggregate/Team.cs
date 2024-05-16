// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.DatabaseContext.Domain.StadiumAggregate;

namespace MyClub.DatabaseContext.Domain.ClubAggregate
{
    public class Team : Entity
    {
        public Team(string name, string shortName)
        {
            Name = name;
            ShortName = shortName;
        }

        public Team(string name, string shortName, Guid id) : base(id)
        {
            Name = name;
            ShortName = shortName;
        }

        public Club Club { get; set; } = null!;

        public Guid ClubId { get; set; }

        public string? Category { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public string? HomeColor { get; set; }

        public string? AwayColor { get; set; }

        public Guid? StadiumId { get; set; }

        public Stadium? Stadium { get; set; }

        public override string ToString() => $"[{Category}] {Club.Name}";

        public override bool IsSimilar(object? obj) => obj is Team other && ClubId == other.ClubId && Category == other.Category;

        public override void SetFrom(object? from)
        {
            if (from is Team team)
            {
                HomeColor = team.HomeColor;
                AwayColor = team.AwayColor;
                StadiumId = team.StadiumId;
                Stadium = team.Stadium;
                Name = team.Name;
                ShortName = team.ShortName;
            }
        }
    }
}
