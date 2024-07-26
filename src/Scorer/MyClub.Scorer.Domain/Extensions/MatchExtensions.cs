// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class MatchExtensions
    {
        private static readonly WeightedRandom<int> TotalGoalsInRegulationTime = new()
        {
            { 0, 0.15 },
            { 1, 0.2 },
            { 2, 0.2 },
            { 3, 0.2 },
            { 4, 0.1 },
            { 5, 0.07 },
            { 6, 0.04 },
            { 7, 0.02 },
            { 8, 0.02 },
        };

        private static readonly WeightedRandom<int> GoalForHomeTeam = new()
        {
            { 0, 0.1 },
            { 1, 0.1 },
            { 2, 0.3 },
            { 3, 0.2 },
            { 4, 0.2 },
            { 5, 0.05 },
            { 6, 0.03 },
            { 7, 0.02 },
        };

        private static readonly WeightedRandom<int> TotalGoalsInExtraTime = new()
        {
            { 0, 0.4 },
            { 1, 0.3 },
            { 2, 0.2 },
            { 3, 0.07 },
            { 4, 0.03 },
        };

        public static void RandomizeScore(this Match match, bool isFinished = true)
        {
            match.Start();

            // Set score in regulation time
            var totalGoalsInRegulationTime = TotalGoalsInRegulationTime.Random();
            var homeScore = GoalForHomeTeam.Filter(x => x <= totalGoalsInRegulationTime).Random();
            var awayScore = totalGoalsInRegulationTime - homeScore;

            EnumerableHelper.Iteration(homeScore, _ => match.Home.AddGoal((int)RandomGenerator.Double(1, match.Format.RegulationTime.GetFullTime(false).TotalMinutes)));
            EnumerableHelper.Iteration(awayScore, _ => match.Away.AddGoal((int)RandomGenerator.Double(1, match.Format.RegulationTime.GetFullTime(false).TotalMinutes)));

            // Set score in extra time
            if (match.Format.ExtraTimeIsEnabled && match.IsDraw())
            {
                match.AfterExtraTime = true;

                var totalGoalsInExtraTime = TotalGoalsInExtraTime.Random();
                var homeExtraTimeScore = GoalForHomeTeam.Filter(x => x <= totalGoalsInExtraTime).Random();
                var awayExtraTimeScore = totalGoalsInExtraTime - homeExtraTimeScore;

                EnumerableHelper.Iteration(homeExtraTimeScore, _ => match.Home.AddGoal((int)RandomGenerator.Double(match.Format.RegulationTime.GetFullTime(false).TotalMinutes, match.Format.GetFullTime(false).TotalMinutes)));
                EnumerableHelper.Iteration(awayExtraTimeScore, _ => match.Away.AddGoal((int)RandomGenerator.Double(match.Format.RegulationTime.GetFullTime(false).TotalMinutes, match.Format.GetFullTime(false).TotalMinutes)));
            }

            // Set score in shootout
            if (match.Format.ShootoutIsEnabled && match.IsDraw())
            {
                match.AfterExtraTime = false;

                EnumerableHelper.Iteration(match.Format.NumberOfPenaltyShootouts ?? 0, _ => match.Home.AddPenaltyShootout(RandomGenerator.Enum<PenaltyShootoutResult>()));
                EnumerableHelper.Iteration(match.Format.NumberOfPenaltyShootouts ?? 0, _ => match.Away.AddPenaltyShootout(RandomGenerator.Enum<PenaltyShootoutResult>()));

                while (match.Home.GetShootoutScore() == match.Away.GetShootoutScore())
                {
                    match.Home.AddPenaltyShootout(RandomGenerator.Enum<PenaltyShootoutResult>());
                    match.Away.AddPenaltyShootout(RandomGenerator.Enum<PenaltyShootoutResult>());
                }
            }

            if (isFinished)
                match.Played();
        }

        public static SchedulingParameters GetSchedulingParameters(this Match match) => match.Parent.ProvideSchedulingParameters();

        public static TimeSpan GetRotationTime(this Match match) => match.GetSchedulingParameters().RotationTime;

        public static TimeSpan GetRestTime(this Match match) => match.GetSchedulingParameters().RestTime;

        public static bool UseHomeVenue(this Match match) => match.GetSchedulingParameters().UseHomeVenue;

        public static bool CanAutomaticReschedule(this Match match) => match.GetSchedulingParameters().AsSoonAsPossible || match.GetSchedulingParameters().DateRules.Count > 0;

        public static bool CanAutomaticRescheduleVenue(this Match match) => match.GetSchedulingParameters().UseHomeVenue || match.GetSchedulingParameters().VenueRules.Count > 0;
    }
}
