// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class TournamentProject : Project<Tournament, TournamentParameters>
    {
        public TournamentProject(string name, DateTime startDate, DateTime endDate, byte[]? image = null, Guid? id = null) : base(CompetitionType.Tournament, name, startDate, endDate, image, id)
        {
        }
    }

    public class LeagueProject : Project<League, LeagueParameters>
    {
        public LeagueProject(string name, DateTime startDate, DateTime endDate, byte[]? image = null, Guid? id = null) : base(CompetitionType.League, name, startDate, endDate, image, id)
        {
        }

        public override Team AddTeam(Team team)
        {
            base.AddTeam(team);
            return Competition.AddTeam(team);
        }

        public override bool RemoveTeam(Team team, bool removeStadium = false)
        {
            base.RemoveTeam(team, removeStadium);
            return Competition.RemoveTeam(team);
        }
    }

    public class CupProject : Project<Cup, CupParameters>
    {
        public CupProject(string name, DateTime startDate, DateTime endDate, byte[]? image = null, Guid? id = null) : base(CompetitionType.Cup, name, startDate, endDate, image, id)
        {
        }
    }

    public abstract class Project<TCompetition, TParameters> : Project, IProject
        where TCompetition : ICompetition, new()
        where TParameters : ProjectParameters, new()
    {
        protected Project(CompetitionType type, string name, DateTime startDate, DateTime endDate, byte[]? image = null, Guid? id = null) : base(type, name, startDate, endDate, image, id) => Competition = new();

        public TCompetition Competition { get; }

        public TParameters Parameters { get; } = new();

        ICompetition IProject.Competition => Competition;

        ProjectParameters IProject.Parameters => Parameters;
    }

    public abstract class Project : AuditableEntity
    {
        private string _name = string.Empty;
        private readonly ObservableCollection<Stadium> _stadiums = [];
        private readonly ObservableCollection<Team> _teams = [];

        protected Project(CompetitionType type, string name, DateTime startDate, DateTime endDate, byte[]? image = null, Guid? id = null) : base(id)
        {
            Type = type;
            Name = name;
            Image = image;
            StartDate = startDate;
            EndDate = endDate;
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

        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

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
