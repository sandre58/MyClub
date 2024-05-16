// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class TournamentParameters : ProjectParameters
    {
    }

    public class CupParameters : ProjectParameters
    {
    }

    public class LeagueParameters : ProjectParameters
    {
    }

    public abstract class ProjectParameters : ValueObject
    {
        public bool UseTeamVenues { get; set; } = true;

        public TimeSpan MatchStartTime { get; set; } = 15.Hours();

        public TimeSpan RotationTime { get; set; } = 1.Days();

        public TimeSpan MinimumRestTime { get; set; } = 2.Days();
    }
}
