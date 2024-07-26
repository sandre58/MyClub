// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{
    public readonly struct AvailableStadiumResult
    {
        public AvailableStadiumResult(Guid? stadiumId, bool isNeutral)
        {
            StadiumId = stadiumId;
            IsNeutral = isNeutral;
        }

        public Guid? StadiumId { get; }

        public bool IsNeutral { get; }
    }
    public interface IAvailableVenueSchedulingRule
    {
        AvailableStadiumResult? GetAvailableStadium(Match match, int index, IEnumerable<Match> scheduledMatches, IEnumerable<Stadium> stadiums);
    }
}

