// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyNet.Utilities;
using MyClub.DatabaseContext.Domain.StadiumAggregate;

namespace MyClub.DatabaseContext.Domain.ClubAggregate
{
    public class Club : Entity
    {
        public Club(string name) => Name = name;

        public Club(Guid id, string name) : base(id) => Name = name;

        public string Name { get; set; }

        public string? Country { get; set; }

        public bool IsNational { get; set; }

        public byte[]? Logo { get; set; }

        public string? HomeColor { get; set; }

        public string? AwayColor { get; set; }

        public Guid? StadiumId { get; set; }

        public Stadium? Stadium { get; set; }

        public string? Description { get; set; }

        public ICollection<Team> Teams { get; set; } = [];

        public override string ToString() => Name;

        public override bool IsSimilar(object? obj) => obj is Club other && Name == other.Name && Country == other.Country;

        public override void SetFrom(object? from)
        {
            if (from is Club club)
            {
                Name = club.Name;
                Country = club.Country;
                Logo = club.Logo;
                HomeColor = club.HomeColor;
                AwayColor = club.AwayColor;
                StadiumId = club.StadiumId;
                Stadium?.SetFrom(club.Stadium);
                Teams.Clear();
                Teams.AddRange(club.Teams);
            }
        }
    }
}
