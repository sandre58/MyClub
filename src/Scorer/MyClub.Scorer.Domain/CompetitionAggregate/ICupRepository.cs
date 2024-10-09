// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.ProjectAggregate
{

    public interface ICupRepository
    {
        bool HasCurrent();

        Cup GetCurrentOrThrow();

        IRound InsertRoundOfMatches(DateTime date, string name, string? shortName = null);

        IRound InsertRoundOfFixtures(string name, string? shortName = null);

        void Fill(IEnumerable<IRound> rounds);
    }
}
