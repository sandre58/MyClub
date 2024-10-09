// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public interface IFixtureResultStrategy
    {
        Result GetResultOf(Fixture fixture, Guid teamId);

        ExtendedResult GetExtendedResultOf(Fixture fixture, Guid teamId);
    }
}
