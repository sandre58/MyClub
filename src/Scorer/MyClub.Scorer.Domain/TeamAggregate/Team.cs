// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public class Team : NameEntity, IAggregateRoot, ITeam
    {
        private readonly ExtendedObservableCollection<Player> _players = [];
        private readonly ExtendedObservableCollection<Manager> _staff = [];

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

        public Player AddPlayer(Player player) => AddPlayers([player]).First();

        public IEnumerable<Player> AddPlayers(IEnumerable<Player> players)
        {
            if (players.FirstOrDefault(Players.Contains) is Player player)
                throw new AlreadyExistsException(nameof(Players), player);

            _players.AddRange(players);

            return players;
        }

        public bool RemovePlayer(Player player) => _players.Remove(player);

        public int RemovePlayers(IEnumerable<Player> players) => players.Count(RemovePlayer);

        #endregion

        #region Managers

        public Manager AddManager(string firstName, string lastName) => AddManager(new Manager(this, firstName, lastName));

        public Manager AddManager(Manager manager) => AddManagers([manager]).First();

        public IEnumerable<Manager> AddManagers(IEnumerable<Manager> managers)
        {
            if (managers.FirstOrDefault(Staff.Contains) is Manager manager)
                throw new AlreadyExistsException(nameof(Staff), manager);

            _staff.AddRange(managers);

            return managers;
        }

        public bool RemoveManager(Manager manager) => _staff.Remove(manager);

        public int RemoveManagers(IEnumerable<Manager> managers) => _staff.Count(RemoveManager);

        #endregion
    }
}
