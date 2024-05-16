// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public abstract class Knockout : AuditableEntity
    {
        private readonly ObservableCollection<Round> _rounds = [];

        protected Knockout(Guid? id = null) : base(id) => Rounds = new(_rounds);

        public ReadOnlyObservableCollection<Round> Rounds { get; }

        public IEnumerable<Match> GetMatches() => Rounds.SelectMany(x => x.Fixtures.SelectMany(x => x.Matches));
    }
}

