// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IStage : IEntity, IMatchesProvider
    {
        IEnumerable<T> GetStages<T>() where T : IStage;

        bool RemoveMatch(Match item);
    }
}
