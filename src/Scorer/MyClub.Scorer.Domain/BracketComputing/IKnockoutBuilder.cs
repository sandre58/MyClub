// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.BracketComputing
{
    public interface IKnockoutBuilder
    {
        IEnumerable<IRound> Build<T>(T stage, IRoundsAlgorithm algorithm) where T : Knockout, ITeamsProvider;
    }
}
