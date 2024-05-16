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
using MyClub.Teamup.Domain.Randomize.Extensions;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Generator.Extensions;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Domain.Randomize
{
    public static class CompetitionRandomFactory
    {
        public static Friendly CreateFriendly(Season season, Category category, IEnumerable<Team> teams, DateTime? startDate = null, DateTime? endDate = null, int numberOfMatches = 5)
        {
            var competition = new Friendly(MyClubResources.Friendlies, MyClubResources.Friendlies.GetInitials(), category);
            competition.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            var seasonCompetition = new FriendlySeason(competition, season, competition.Rules, startDate ?? DateTime.Today.BeginningOfYear(), endDate ?? DateTime.Today.EndOfYear());
            seasonCompetition.SetTeams(teams);

            var oppositions = teams.RoundRobin().SelectMany(x => x).ToList();
            var randomOppositions = RandomGenerator.ListItems(oppositions, Math.Min(numberOfMatches, oppositions.Count));
            var date = seasonCompetition.Period.Start;
            randomOppositions.ForEach(x =>
            {
                date = date = date.AddDays(3).AddFluentTimeSpan(seasonCompetition.Rules.MatchTime);
                seasonCompetition.AddMatch(date, x.item1, x.item2);
            });

            seasonCompetition.GetAllMatches().ForEach(x => x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System));
            seasonCompetition.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
            competition.AddSeason(seasonCompetition);

            return competition;
        }

        public static League CreateLeague(Season season, Category category, IEnumerable<Team> teams, string? name = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var promotion = new RankLabel(RandomGenerator.Color(), MyClubResources.Promotion, MyClubResources.PromotionAbbr);
            var relegation = new RankLabel(RandomGenerator.Color(), MyClubResources.Relegation, MyClubResources.RelegationAbbr);

            var rules = new Dictionary<AcceptableValueRange<int>, RankLabel>()
            {
                { new AcceptableValueRange<int>(1, RandomGenerator.Int(1, 3)), promotion },
                { new AcceptableValueRange<int>(teams.Count() - RandomGenerator.Int(0, 3), teams.Count()), relegation }
            };

            var rankingRules = new RankingRules(3, 1, 0, RankingRules.DefaultSortingColumns, rules);
            var competitionName = name ?? $"{MyClubResources.League} {NameGenerator.LastName()}";
            var competition = new League(competitionName, competitionName.GetInitials(), category, new LeagueRules(rankingRules: rankingRules));
            competition.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            var seasonCompetition = new LeagueSeason(competition, season, competition.Rules, startDate ?? DateTime.Today.BeginningOfYear(), endDate ?? DateTime.Today.EndOfYear());
            seasonCompetition.SetTeams(teams);
            seasonCompetition.ComputeMatchdays(new MatchdaysBuilder() { StartDate = seasonCompetition.Period.Start, Time = seasonCompetition.Rules.MatchTime });
            seasonCompetition.GetAllMatches().ForEach(x => x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System));
            seasonCompetition.Matchdays.ForEach(x => x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System));

            seasonCompetition.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
            competition.AddSeason(seasonCompetition);

            return competition;
        }

        public static Cup CreateCup(Season season, Category category, IEnumerable<Team> teams, string? name = null, DateTime? startDate = null, DateTime? endDate = null, int numberOfRounds = 5)
        {
            var competitionName = name ?? $"{MyClubResources.Cup} {NameGenerator.FullName()}";
            var competition = new Cup(competitionName, competitionName.GetInitials(), category);
            competition.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            var seasonCompetition = new CupSeason(competition, season, competition.Rules, startDate ?? DateTime.Today.BeginningOfYear(), endDate ?? DateTime.Today.EndOfYear());
            seasonCompetition.SetTeams(teams);

            var firstGroundIsGroupStage = RandomGenerator.Bool();

            var roundStartDate = seasonCompetition.Period.Start;
            var roundName = 1.ToString(MyClubResources.RoundX);
            if (firstGroundIsGroupStage)
            {
                var roundEndDate = roundStartDate.AddDays(RandomGenerator.Int(50, 70));
                var groupStage = new GroupStage(roundName, roundName.GetInitials(), roundStartDate, roundEndDate);
                groupStage.SetTeams(teams);
                groupStage.ComputeRandomGroups();
                roundStartDate = roundEndDate.AddDays(RandomGenerator.Int(7, 30));
                seasonCompetition.AddRound(groupStage);
            }
            else
            {
                var knockout = new Knockout(roundName, roundName.GetInitials(), roundStartDate.AddFluentTimeSpan(competition.Rules.MatchTime), competition.Rules);
                knockout.SetTeams(teams);
                seasonCompetition.AddRound(knockout);

                CompetitionExtensions.ComputeKnockoutMatches(knockout.Teams, knockout.Date, knockout.Rules.MatchFormat).ForEach(x => knockout.AddMatch(x));
            }

            for (var i = 2; i <= numberOfRounds; i++)
            {
                roundStartDate = roundStartDate.AddDays(RandomGenerator.Int(15, 30));
                roundName = i.ToString(MyClubResources.RoundX);
                var knockout = new Knockout(roundName, roundName.GetInitials(), roundStartDate.AddFluentTimeSpan(competition.Rules.MatchTime), competition.Rules);
                seasonCompetition.AddRound(knockout);
            }

            seasonCompetition.GetAllMatches().ForEach(x => x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System));
            seasonCompetition.Rounds.ForEach(x => x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System));
            seasonCompetition.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
            competition.AddSeason(seasonCompetition);

            return competition;
        }
    }
}
