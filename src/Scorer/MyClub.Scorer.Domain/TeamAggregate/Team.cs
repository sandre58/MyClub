// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public class Team : NameEntity, IAggregateRoot, ITeam
    {
        private readonly ObservableCollection<Player> _players = [];
        private readonly ObservableCollection<Manager> _staff = [];

        public Team(string name, string? shortName = null, Guid? id = null) : base(name, shortName ?? name.GetInitials(), id)
        {
            Players = new(_players);
            Staff = new(_staff);
        }

        public byte[]? Logo { get; set; }

        public Country? Country { get; set; }

        public Stadium? Stadium { get; set; }

        public string? HomeColor { get; set; }

        public string? AwayColor { get; set; }

        public ReadOnlyObservableCollection<Player> Players { get; }

        public ReadOnlyObservableCollection<Manager> Staff { get; }

        #region Players

        public Player AddPlayer(string firstName, string lastName) => AddPlayer(new Player(this, firstName, lastName));

        public Player AddPlayer(Player player)
        {
            if (Players.Contains(player))
                throw new AlreadyExistsException(nameof(Players), player);

            _players.Add(player);

            return player;
        }

        public bool RemovePlayer(Player player) => _players.Remove(player);

        #endregion

        #region Managers

        public Manager AddManager(string firstName, string lastName) => AddManager(new Manager(this, firstName, lastName));

        public Manager AddManager(Manager manager)
        {
            if (Staff.Contains(manager))
                throw new AlreadyExistsException(nameof(Staff), manager);

            _staff.Add(manager);

            return manager;
        }

        public bool RemoveManager(Manager manager) => _staff.Remove(manager);

        #endregion
    }
}
