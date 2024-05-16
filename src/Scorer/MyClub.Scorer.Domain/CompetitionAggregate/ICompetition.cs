// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface ICompetition : IEntity, IAggregateRoot, IMatchFormatProvider
    {
        IEnumerable<IMatchdaysProvider> GetAllMatchdaysProviders();

        IEnumerable<IMatchesProvider> GetAllMatchesProviders();
    }
}

