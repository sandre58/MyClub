// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Domain.TeamAggregate
{
    public class Team : NameEntity, IAggregateRoot
    {
        public Team(Club club, Category category, string name, string? shortName = null, Guid? id = null) : base(name, shortName ?? name.GetInitials(), id)
        {
            Club = club;
            Category = category;

            HomeColor.Initialize(club, () => club.HomeColor);
            AwayColor.Initialize(club, () => club.AwayColor);
            Stadium.Initialize(club, () => club.Stadium);

            HomeColor.PropertyChanged += (sender, e) => (e.PropertyName == nameof(OverridableValue<string?>.Value)).IfTrue(() => RaisePropertyChanged(nameof(HomeColor)));
            AwayColor.PropertyChanged += (sender, e) => (e.PropertyName == nameof(OverridableValue<string?>.Value)).IfTrue(() => RaisePropertyChanged(nameof(AwayColor)));
            Stadium.PropertyChanged += (sender, e) => (e.PropertyName == nameof(OverridableValue<Stadium?>.Value)).IfTrue(() => RaisePropertyChanged(nameof(Stadium)));
        }

        public Club Club { get; }

        public Category Category { get; }

        public OverridableValue<string?> HomeColor { get; private set; } = new();

        public OverridableValue<string?> AwayColor { get; private set; } = new();

        public OverridableValue<Stadium?> Stadium { get; private set; } = new();

        public int Order { get; set; }

        public override int CompareTo(object? obj)
        {
            if (obj is Team other)
            {
                var value = Club.CompareTo(other.Club);

                if (value != 0) return value;

                value = Category.CompareTo(other.Category);

                if (value != 0) return value;

                value = Order.CompareTo(other.Order);

                return value != 0 ? value : string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }

            return 1;
        }
    }
}
