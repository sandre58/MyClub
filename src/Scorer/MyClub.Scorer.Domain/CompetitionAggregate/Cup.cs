// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Cup : Knockout, ICompetition
    {
        public Cup() { }

        public MatchFormat MatchFormat { get; set; } = MatchFormat.NoDraw;

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        public IEnumerable<IMatchdaysProvider> GetAllMatchdaysProviders() => [];

        public IEnumerable<IMatchesProvider> GetAllMatchesProviders() => Rounds.SelectMany(x => x.Fixtures);
    }
}

