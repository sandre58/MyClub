// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Application.Dtos
{
    public class BuildParametersDto
    {
        public bool UseRoundRobin { get; set; }

        public int NumberOfMatchesByTeam { get; set; }

        public int NumberOfMatchesBetweenTeams { get; set; }

        public bool UseHomeTeamVenues { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
