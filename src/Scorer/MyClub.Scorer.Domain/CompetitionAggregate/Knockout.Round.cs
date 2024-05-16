// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public abstract class Round : NameEntity, IMatchFormatProvider
    {
        private readonly ObservableCollection<ITeam> _teams = [];
        private readonly ObservableCollection<Fixtures> _fixtures = [];

        protected Round(string name, string shortName, MatchFormat? matchFormat = null, Guid? id = null) : base(name, shortName, id)
        {
            MatchFormat = matchFormat ?? MatchFormat.NoDraw;
            Teams = new(_teams);
            Fixtures = new(_fixtures);
        }

        public MatchFormat MatchFormat { get; set; }

        public ReadOnlyObservableCollection<ITeam> Teams { get; }

        public ReadOnlyObservableCollection<Fixtures> Fixtures { get; }

        public MatchFormat ProvideFormat() => MatchFormat;
    }
}
