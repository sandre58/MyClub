// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;


namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public abstract class Competition<TRules, TSeason> : NameEntity, IAggregateRoot, ICompetition
        where TRules : CompetitionRules
        where TSeason : ICompetitionSeason
    {
        private readonly ObservableCollection<TSeason> _seasons = [];

        protected Competition(string name, string shortName, Category category, TRules rules, Guid? id = null) : base(name, shortName, id)
        {
            Category = category;
            Rules = rules;
            Seasons = new(_seasons);
        }

        public Category Category { get; }

        public byte[]? Logo { get; set; }

        public TRules Rules { get; set; }

        CompetitionRules ICompetition.Rules
        {
            get => Rules;
            set => Rules = (TRules)value;
        }

        public ReadOnlyObservableCollection<TSeason> Seasons { get; }

        IEnumerable<ICompetitionSeason> ICompetition.Seasons => Seasons.OfType<ICompetitionSeason>();

        #region Seasons

        public TSeason AddSeason(TSeason season)
        {
            if (Seasons.Any(x => x.Season == season.Season))
                throw new AlreadyExistsException(nameof(Seasons), season);

            _seasons.Add(season);

            return season;
        }

        public bool RemoveSeason(TSeason season) => _seasons.Remove(season);

        bool ICompetition.RemoveSeason(ICompetitionSeason season) => RemoveSeason((TSeason)season);

        ICompetitionSeason ICompetition.AddSeason(ICompetitionSeason season) => AddSeason((TSeason)season);

        #endregion
    }
}

