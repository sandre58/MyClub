// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.Factories;
using MyClub.Teamup.Domain.Factories.Extensions;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Helpers;

namespace MyClub.Teamup.Domain.Randomize.Extensions
{
    public static class CompetitionExtensions
    {
        public static IEnumerable<Match> ComputeKnockoutMatches(IEnumerable<Team> teams, DateTime date, MatchFormat matchFormat)
        {
            var availableTeams = teams.ToList();
            while (availableTeams.Count > 1)
            {
                var teamsOfMatch = RandomGenerator.ListItems(availableTeams, 2);
                var match = new Match(date, teamsOfMatch[0], teamsOfMatch[teamsOfMatch.Count - 1], matchFormat);
                teamsOfMatch.ForEach(x => availableTeams.Remove(x));

                yield return match;
            }
        }

        public static void ComputeRandomGroups(this GroupStage groupStage, int teamsByGroup = 4)
        {
            var availableTeams = groupStage.Teams.ToList();
            var numberOfGroups = Math.Ceiling((double)(availableTeams.Count - 1) / teamsByGroup);

            for (var i = 1; i <= numberOfGroups; i++)
            {
                var groupTeams = RandomGenerator.ListItems(availableTeams, Math.Min(4, availableTeams.Count)).ToList();
                var groupName = MyClubResources.GroupX.FormatWith(i);
                var group = new Group(groupStage, groupName, groupName.GetInitials());
                group.SetTeams(groupTeams);
                groupStage.AddGroup(group);

                groupTeams.ForEach(x => availableTeams.Remove(x));
            }
            groupStage.ComputeMatchdays(new MatchdaysBuilder() { StartDate = groupStage.Period.Start, Time = groupStage.Rules.MatchTime });
            groupStage.Matchdays.ForEach(x => x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System));
        }

        public static void RandomizeScore(this Match match)
        {
            if (match.Date.IsInPast())
            {
                match.Start();

                // Set score in regulation time
                var homeScore = RandomGenerator.Int(0, 4);
                var awayScore = RandomGenerator.Int(0, 4);

                EnumerableHelper.Iteration(homeScore, _ => match.Home.AddGoal((int)RandomGenerator.Double(1, match.Format.RegulationTime.GetFullTime().TotalMinutes)));
                EnumerableHelper.Iteration(awayScore, _ => match.Away.AddGoal((int)RandomGenerator.Double(1, match.Format.RegulationTime.GetFullTime().TotalMinutes)));

                // Set score in extra time
                if (match.Format.ExtraTimeIsEnabled && match.IsDraw())
                {
                    match.AfterExtraTime = true;

                    var homeExtraTimeScore = RandomGenerator.Int(0, 2);
                    var awayExtraTimeScore = RandomGenerator.Int(0, 2);

                    EnumerableHelper.Iteration(homeExtraTimeScore, _ => match.Home.AddGoal((int)RandomGenerator.Double(match.Format.RegulationTime.GetFullTime().TotalMinutes, match.Format.GetFullTime().TotalMinutes)));
                    EnumerableHelper.Iteration(awayExtraTimeScore, _ => match.Away.AddGoal((int)RandomGenerator.Double(match.Format.RegulationTime.GetFullTime().TotalMinutes, match.Format.GetFullTime().TotalMinutes)));
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

                match.Played();
            }
        }
    }
}
