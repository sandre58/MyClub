// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.ProjectAggregate
{

    public interface ICupRepository
    {
        bool HasCurrent();

        Cup GetCurrentOrThrow();

        //Round InsertRound(IRoundFormat format, DateTime[] dates, string name, string? shortName = null);

        void Fill(IEnumerable<Round> rounds);
    }
}
