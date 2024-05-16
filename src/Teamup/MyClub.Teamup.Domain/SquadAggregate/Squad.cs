// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Domain.SquadAggregate
{
    public class Squad : LabelEntity, IAggregateRoot, ISimilar, IEnumerable<SquadPlayer>
    {
        private readonly ObservableCollection<SquadPlayer> _players = [];

        public Squad(Club club, Season season, Category category, string? label = null, Guid? id = null) : base(label ?? $"{category} {season}", id)
        {
            Club = club;
            Season = season;
            Category = category;
            Players = new(_players);
        }

        public Club Club { get; }

        public Season Season { get; }

        public Category Category { get; }

        public ReadOnlyObservableCollection<SquadPlayer> Players { get; }

        #region Players

        public SquadPlayer AddPlayer(SquadPlayer player)
        {
            if (Players.Any(x => x.Player == player.Player))
                throw new AlreadyExistsException(nameof(Players), player);

            _players.Add(player);

            return player;
        }

        public SquadPlayer AddPlayer(Player player) => AddPlayer(new SquadPlayer(player));

        public bool RemovePlayer(SquadPlayer player) => _players.Remove(player);

        public void ClearPlayers() => _players.Clear();

        #endregion

        public override int CompareTo(object? obj)
        {
            if (obj is Squad other)
            {
                var value = Club.CompareTo(other.Club);

                if (value != 0) return value;

                value = Season.CompareTo(other.Season);

                if (value != 0) return value;

                value = Category.CompareTo(other.Category);

                if (value != 0) return value;

                value = Order.CompareTo(other.Order);

                return value != 0 ? value : string.Compare(Label, other.Label, StringComparison.OrdinalIgnoreCase);
            }

            return 1;
        }

        public override bool IsSimilar(object? obj)
            => obj is Squad other && ReferenceEquals(Club, other.Club) && Season == other.Season && Category == other.Category && Label.OrEmpty().Equals(other.Label, StringComparison.OrdinalIgnoreCase);

        public IEnumerator<SquadPlayer> GetEnumerator() => Players.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
