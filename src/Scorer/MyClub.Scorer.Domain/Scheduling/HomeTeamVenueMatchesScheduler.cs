﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class HomeTeamVenueMatchesScheduler : IMatchesScheduler
    {
        public static readonly HomeTeamVenueMatchesScheduler Default = new();

        public void Schedule(IEnumerable<Match> matches)
        {
            foreach (var match in matches)
            {
                match.Stadium = match.HomeTeam.Stadium;
                match.IsNeutralStadium = false;
            }
        }
    }
}

