// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.Enums;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public interface IMatchDomainService
    {
        IEnumerable<ConflictType> GetConflictsBetween(Match match1, Match match2);

        IEnumerable<(ConflictType, Match, Match?)> GetAllConflicts();
    }
}
