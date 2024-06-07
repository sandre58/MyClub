// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Application.Dtos
{
    public class ProjectParametersDto
    {
        public bool UseTeamVenues { get; set; }

        public TimeSpan MatchStartTime { get; set; }

        public TimeSpan RotationTime { get; set; }

        public TimeSpan MinimumRestTime { get; set; }
    }
}
