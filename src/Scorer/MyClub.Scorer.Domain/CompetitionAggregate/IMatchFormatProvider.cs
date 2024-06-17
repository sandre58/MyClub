﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IMatchFormatProvider
    {
        MatchFormat ProvideFormat();
    }
}
