// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;
using MyNet.Utilities.Extensions;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public abstract class Project<TCompetition> : Project, IProject
        where TCompetition : ICompetition, new()
    {
        protected Project(CompetitionType type, string name, byte[]? image = null, Guid? id = null)
            : base(type, name, image, id) => Competition = new();

        public TCompetition Competition { get; }

        ICompetition IProject.Competition => Competition;
    }

    public abstract class Project : AuditableEntity
    {
        private string _name = string.Empty;
        private readonly ExtendedObservableCollection<Stadium> _stadiums = [];
        private readonly ExtendedObservableCollection<Team> _teams = [];

        protected Project(CompetitionType type, string name, byte[]? image = null, Guid? id = null) : base(id)
        {
            Type = type;
            Name = name;
            Image = image;
            Teams = new(_teams);
            Stadiums = new(_stadiums);
        }

        public CompetitionType Type { get; }

        public string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public byte[]? Image { get; set; }

        public ReadOnlyObservableCollection<Team> Teams { get; }

        public ReadOnlyObservableCollection<Stadium> Stadiums { get; }

        public override string ToString() => Name;

        #region Teams

        public virtual Team AddTeam(Team team)
        {
            if (Teams.Contains(team))
                throw new AlreadyExistsException(nameof(Teams), team);

            _teams.Add(team);

            return team;
        }

        public virtual bool RemoveTeam(Team team, bool removeStadium = false)
        {
            if (removeStadium && team.Stadium is not null)
                RemoveStadium(team.Stadium);

            return _teams.Remove(team);
        }

        #endregion

        #region Stadiums

        public Stadium AddStadium(Stadium stadium)
        {
            if (Stadiums.Contains(stadium))
                throw new AlreadyExistsException(nameof(Stadium), stadium);

            _stadiums.Add(stadium);

            return stadium;
        }

        public bool RemoveStadium(Stadium stadium)
        {
            RemoveStadiumOnCascade(stadium);

            return _stadiums.Remove(stadium);
        }

        private void RemoveStadiumOnCascade(Stadium stadium)
            => Teams.ForEach(x =>
            {
                if (x.Stadium?.Id == stadium.Id)
                    x.Stadium = null;
            });

        #endregion Stadiums
    }
}
