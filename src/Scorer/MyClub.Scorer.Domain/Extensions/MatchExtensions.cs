// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
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

        private static readonly WeightedRandom<GoalType> GoalTypes = new()
        {
            { GoalType.Other, 0.8 },
            { GoalType.Penalty, 0.1 },
            { GoalType.FreeKick, 0.07 },
            { GoalType.OwnGoal, 0.03 }
        };

        private static readonly WeightedRandom<bool> HasAssist = new()
        {
            { true, 0.7 },
            { false, 0.3 }
        };

        private static readonly WeightedRandom<PenaltyShootoutResult> ShootoutResult = new()
        {
            { PenaltyShootoutResult.None, 0 },
            { PenaltyShootoutResult.Succeeded, 0.8 },
            { PenaltyShootoutResult.Failed, 0.2 },
        };

        private static readonly WeightedRandom<int> TotalCardsInRegulationTime = new()
        {
            { 0, 0.05 },
            { 1, 0.05 },
            { 2, 0.1 },
            { 3, 0.1 },
            { 4, 0.12 },
            { 5, 0.12 },
            { 6, 0.12 },
            { 7, 0.12 },
            { 8, 0.12 },
            { 9, 0.04 },
            { 10, 0.02 },
            { 11, 0.02 },
            { 12, 0.02 },
        };

        private static readonly WeightedRandom<int> TotalCardsInExtraTime = new()
        {
            { 0, 0.4 },
            { 1, 0.3 },
            { 2, 0.2 },
            { 3, 0.07 },
            { 4, 0.03 },
        };

        private static readonly WeightedRandom<CardColor> CardColors = new()
        {
            { CardColor.Yellow, 0.55 },
            { CardColor.Red, 0.15 },
            { CardColor.Black, 0 },
            { CardColor.Green, 0 },
            { CardColor.White, 0.3 },
        };

        public static ExtendedResult GetExtendedResultOf(this Match match, Team team) => match.GetExtendedResultOf(team.Id);

        public static bool IsWithdrawn(this Match match, Team team) => match.IsWithdrawn(team.Id);

        public static int GoalsFor(this Match match, Team team) => match.GoalsFor(team.Id);

        public static int GoalsAgainst(this Match match, Team team) => match.GoalsAgainst(team.Id);

        public static MatchOpponent? GetOpponent(this Match match, Team team) => match.GetOpponent(team.Id);

        public static MatchOpponent? GetOpponentAgainst(this Match match, Team team) => match.GetOpponentAgainst(team.Id);

        public static void Randomize(this Match match, bool isFinished = true)
        {
            if (match is MatchOfFixture matchOfFixture)
            {
                if (matchOfFixture.Fixture.Stage.FixtureFormat.UseShootout == Enums.NoDrawUsage.OnLastMatch || matchOfFixture.Fixture.Stage.FixtureFormat.UseExtraTime == Enums.NoDrawUsage.OnLastMatch)
                    RandomizeMatch(matchOfFixture, isFinished);
                else
                    RandomizeMatch(match, isFinished);
            }
            else
                RandomizeMatch(match, isFinished);
        }

        private static void RandomizeMatch(this Match match, bool isFinished = true)
        {
            if (match.Home is null || match.Away is null) return;

            match.Home.Reset();
            match.Away.Reset();

            // Set score in regulation time
            var totalGoalsInRegulationTime = TotalGoalsInRegulationTime.Random();
            var homeScore = GoalForHomeTeam.Filter(x => x <= totalGoalsInRegulationTime).Random();
            var awayScore = totalGoalsInRegulationTime - homeScore;

            EnumerableHelper.Iteration(homeScore, _ => match.Home.AddGoal(RandomizeGoal(match.Format, match.Home.Team, false)));
            EnumerableHelper.Iteration(awayScore, _ => match.Away.AddGoal(RandomizeGoal(match.Format, match.Away.Team, false)));

            // Set cards in regulation time
            var totalCardsInRegulationTime = TotalCardsInRegulationTime.Random();
            var homeCards = RandomGenerator.Int(0, totalCardsInRegulationTime);
            var awayCards = totalCardsInRegulationTime - homeCards;

            EnumerableHelper.Iteration(homeCards, _ => match.Home.AddCard(RandomizeCard(match.Format, match.Rules, match.Home.Team, false)));
            EnumerableHelper.Iteration(awayCards, _ => match.Away.AddCard(RandomizeCard(match.Format, match.Rules, match.Away.Team, false)));

            if (isFinished)
                match.Played();
            else
                match.Start();

            // Set score in extra time
            match.AfterExtraTime = match.UseExtraTime();
            if (match.AfterExtraTime)
            {
                var totalGoalsInExtraTime = TotalGoalsInExtraTime.Random();
                var homeExtraTimeScore = GoalForHomeTeam.Filter(x => x <= totalGoalsInExtraTime).Random();
                var awayExtraTimeScore = totalGoalsInExtraTime - homeExtraTimeScore;

                EnumerableHelper.Iteration(homeExtraTimeScore, _ => match.Home.AddGoal(RandomizeGoal(match.Format, match.Home.Team, true)));
                EnumerableHelper.Iteration(awayExtraTimeScore, _ => match.Away.AddGoal(RandomizeGoal(match.Format, match.Away.Team, true)));

                // Set cards in regulation time
                var totalCardsInExtraTime = TotalCardsInExtraTime.Random();
                var homeCardsinExtraTime = RandomGenerator.Int(0, totalCardsInExtraTime);
                var awayCardsinExtraTime = totalCardsInExtraTime - homeCardsinExtraTime;

                EnumerableHelper.Iteration(homeCardsinExtraTime, _ => match.Home.AddCard(RandomizeCard(match.Format, match.Rules, match.Home.Team, true)));
                EnumerableHelper.Iteration(awayCardsinExtraTime, _ => match.Away.AddCard(RandomizeCard(match.Format, match.Rules, match.Away.Team, true)));
            }

            // Set score in shootout
            if (match.UseShootout())
            {
                match.AfterExtraTime = false;

                EnumerableHelper.Iteration(match.Format.NumberOfPenaltyShootouts ?? 0, _ => match.Home.AddPenaltyShootout(RandomizePenaltyShootout(match.Home.Team)));
                EnumerableHelper.Iteration(match.Format.NumberOfPenaltyShootouts ?? 0, _ => match.Away.AddPenaltyShootout(RandomizePenaltyShootout(match.Away.Team)));

                while (match.Home.GetShootoutScore() == match.Away.GetShootoutScore())
                {
                    match.Home.AddPenaltyShootout(RandomizePenaltyShootout(match.Home.Team));
                    match.Away.AddPenaltyShootout(RandomizePenaltyShootout(match.Away.Team));
                }
            }
        }

        private static bool UseExtraTime(this Match match)
            => match.Format.ExtraTimeIsEnabled
                && (match is MatchOfFixture matchOfFixture
                    ? matchOfFixture.Fixture.Stage.FixtureFormat.UseExtraTime switch
                    {
                        NoDrawUsage.OnAllMatches => match.IsDraw(),
                        NoDrawUsage.OnLastMatch => (matchOfFixture.Fixture.Stage.Stages.LastOrDefault()?.Matches.Contains(match)).IsTrue() && matchOfFixture.Fixture.GetWinner() is null,
                        _ => false,
                    }
                    : match.IsDraw());

        private static bool UseShootout(this Match match)
            => match.Format.ShootoutIsEnabled
                && (match is MatchOfFixture matchOfFixture
                    ? matchOfFixture.Fixture.Stage.FixtureFormat.UseShootout switch
                    {
                        NoDrawUsage.OnAllMatches => match.IsDraw(),
                        NoDrawUsage.OnLastMatch => (matchOfFixture.Fixture.Stage.Stages.LastOrDefault()?.Matches.Contains(match)).IsTrue() && matchOfFixture.Fixture.GetWinner() is null,
                        _ => false,
                    }
                    : match.IsDraw());

        private static Goal RandomizeGoal(MatchFormat format, Team team, bool inExtraTime)
        {
            var type = GoalTypes.Random();
            var minute = inExtraTime
                         ? (int)RandomGenerator.Double(format.RegulationTime.GetFullTime(false).TotalMinutes, format.GetFullTime(false).TotalMinutes)
                         : (int)RandomGenerator.Double(1, format.RegulationTime.GetFullTime(false).TotalMinutes);
            var scorer = type != GoalType.OwnGoal
                        ? RandomizePlayer(team.Players)
                        : null;
            var assist = type == GoalType.Other && HasAssist.Random()
                        ? RandomizePlayer(team.Players.Except([scorer!]).ToList())
                        : null;

            return new Goal(type, scorer, assist, minute);
        }

        private static PenaltyShootout RandomizePenaltyShootout(Team team) => new(RandomizePlayer(team.Players), ShootoutResult.Random());

        private static Card RandomizeCard(MatchFormat format, MatchRules rules, Team team, bool inExtraTime)
        {
            var color = CardColors.Filter(x => rules.AllowedCards.Contains(x)).Random();
            var minute = inExtraTime
                         ? (int)RandomGenerator.Double(format.RegulationTime.GetFullTime(false).TotalMinutes, format.GetFullTime(false).TotalMinutes)
                         : (int)RandomGenerator.Double(1, format.RegulationTime.GetFullTime(false).TotalMinutes);
            var player = RandomizePlayer(team.Players);
            var infraction = RandomGenerator.Enum<CardInfraction>();

            return new Card(color, player, infraction, minute);
        }

        private static Player? RandomizePlayer(IList<Player> players) => players.Count > 0 ? RandomGenerator.ListItem(players) : null;

        public static IMatchesProvider GetStage(this Match match)
            => match switch
            {
                MatchOfMatchday matchOfMatchday => matchOfMatchday.Stage,
                MatchOfRound matchOfFixture => matchOfFixture.Stage,
                _ => throw new InvalidOperationException("No stage defined for this match"),
            };

        public static SchedulingParameters GetSchedulingParameters(this Match match)
            => match switch
            {
                MatchOfMatchday matchOfMatchday => matchOfMatchday.Stage.ProvideSchedulingParameters(),
                MatchOfRound matchOfFixture => matchOfFixture.Stage.ProvideSchedulingParameters(),
                _ => throw new InvalidOperationException("No stage defined for this match"),
            };

        public static TimeSpan GetRotationTime(this Match match) => match.GetSchedulingParameters().RotationTime;

        public static TimeSpan GetRestTime(this Match match) => match.GetSchedulingParameters().RestTime;

        public static bool UseHomeVenue(this Match match) => match.GetSchedulingParameters().UseHomeVenue;

        public static bool CanAutomaticReschedule(this Match match) => match.GetSchedulingParameters().CanAutomaticReschedule();

        public static bool CanAutomaticRescheduleVenue(this Match match) => match.GetSchedulingParameters().CanAutomaticRescheduleVenue();
    }
}
