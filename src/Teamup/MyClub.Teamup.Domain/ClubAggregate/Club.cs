// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Domain.ClubAggregate
{
    public class Club : NameEntity, IAggregateRoot
    {
        private readonly ObservableCollection<Team> _teams = [];

        public Club(string name, Guid? id = null) : base(name, name, id) => Teams = new(_teams);

        public byte[]? Logo { get; set; }

        public Country? Country { get; set; }

        public Stadium? Stadium { get; set; }

        public string? HomeColor { get; set; }

        public string? AwayColor { get; set; }

        public ReadOnlyObservableCollection<Team> Teams { get; }

        #region Teams

        public Team AddTeam(Team team)
        {
            if (!ReferenceEquals(this, team.Club))
                throw new InvalidOperationException($"{team} cannot add to this club");

            if (Teams.Any(x => x.IsSimilar(team)))
                throw new AlreadyExistsException(nameof(Teams), team);

            _teams.Add(team);

            return team;
        }

        public Team AddTeam(Category category, string? name = null, string? shortName = null) => AddTeam(new Team(this, category, name ?? ShortName, shortName));

        public bool RemoveTeam(Team team) => _teams.Remove(team);

        #endregion
    }
}
