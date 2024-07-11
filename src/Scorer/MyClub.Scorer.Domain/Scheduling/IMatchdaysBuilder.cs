// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface IMatchdaysBuilder
    {
        IEnumerable<Matchday> Build(IMatchdaysProvider matchdaysProvider, IMatchdaysAlgorithm algorithm);
    }
}
