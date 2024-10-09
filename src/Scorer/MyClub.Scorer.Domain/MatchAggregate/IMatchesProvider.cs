// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public interface IMatchesProvider
    {
        IEnumerable<Match> GetAllMatches();
    }
}
