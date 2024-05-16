// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;
using MyClub.Teamup.Domain.MatchAggregate;


namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class CompetitionRules : ValueObject
    {
        public CompetitionRules(MatchFormat matchFormat, TimeSpan matchTime)
        {
            MatchFormat = matchFormat;
            MatchTime = matchTime;
        }

        public MatchFormat MatchFormat { get; }

        public TimeSpan MatchTime { get; }
    }
}

