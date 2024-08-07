﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Infrastructure.Packaging.Models;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Infrastructure.Packaging.Converters
{
    public static class ToEntityConverters
    {
        public static IProject CreateProject(this ProjectPackage source)
        {
            var type = (CompetitionType)source.Metadata!.Type;
            IProject project = type switch
            {
                CompetitionType.League => new LeagueProject(source.Metadata!.Name, source.Metadata!.Image, source.Metadata!.Id),
                CompetitionType.Cup => new CupProject(source.Metadata!.Name, source.Metadata!.Image, source.Metadata!.Id),
                CompetitionType.Tournament => new TournamentProject(source.Metadata!.Name, source.Metadata!.Image, source.Metadata!.Id),
                _ => throw new InvalidOperationException("Project type unknown"),
            };

            var stadiums = source.Stadiums?.Select(x => x.CreateStadium()).ToArray() ?? [];
            var teams = source.Teams?.Select(x => x.CreateTeam(x.StadiumId.HasValue ? stadiums.GetByIdOrDefault(x.StadiumId.Value) : null)).ToArray() ?? [];

            stadiums.ForEach(x => project.AddStadium(x));
            teams.ForEach(x => project.AddTeam(x));
            project.MarkedAsCreated(source.Metadata!.CreatedAt, source.Metadata!.CreatedBy);
            project.MarkedAsModified(source.Metadata!.ModifiedAt, source.Metadata!.ModifiedBy);

            // Competition
            switch (project.Competition)
            {
                case League league:
                    var leaguePackage = (LeaguePackage)source.Competition!;
                    league.SchedulingParameters = leaguePackage.SchedulingParameters?.CreateSchedulingParameters() ?? SchedulingParameters.Default;
                    league.Labels.AddRange(leaguePackage.Labels?.ToDictionary(x => new AcceptableValueRange<int>(x.StartRank, x.EndRank), x => new RankLabel(x.Color, x.Name.OrEmpty(), x.ShortName.OrEmpty(), x.Description)));
                    league.RankingRules = leaguePackage.RankingRules?.CreateRankingRules() ?? RankingRules.Default;
                    league.MatchFormat = leaguePackage.MatchFormat?.CreateMatchFormat() ?? MatchFormat.Default;
                    leaguePackage.Matchdays?.ForEach(x => league.AddMatchday(x.CreateMatchday(league, teams, stadiums, teams.SelectMany(y => y.Players).ToList())));

                    league.MarkedAsCreated(leaguePackage.CreatedAt, leaguePackage.CreatedBy);
                    league.MarkedAsModified(leaguePackage.ModifiedAt, leaguePackage.ModifiedBy);
                    break;

                case Cup cup:
                    var cupPackage = (CupPackage)source.Competition!;
                    cup.MarkedAsCreated(cupPackage.CreatedAt, cupPackage.CreatedBy);
                    cup.MarkedAsModified(cupPackage.ModifiedAt, cupPackage.ModifiedBy);
                    break;

                case Tournament tournament:
                    var tournamentPackage = (TournamentPackage)source.Competition!;
                    tournament.MarkedAsCreated(tournamentPackage.CreatedAt, tournamentPackage.CreatedBy);
                    tournament.MarkedAsModified(tournamentPackage.ModifiedAt, tournamentPackage.ModifiedBy);
                    break;
            }

            return project;
        }

        public static SchedulingParameters? CreateSchedulingParameters(this SchedulingParametersPackage schedulingParametersPackage)
            => new(schedulingParametersPackage.StartDate,
                   schedulingParametersPackage.EndDate,
                   schedulingParametersPackage.StartTime,
                   schedulingParametersPackage.RotationTime,
                   schedulingParametersPackage.RestTime,
                   schedulingParametersPackage.UseHomeVenue,
                   schedulingParametersPackage.AsSoonAsPossible,
                   schedulingParametersPackage.Interval,
                   schedulingParametersPackage.ScheduleByParent,
                   schedulingParametersPackage.AsSoonAsPossibleRules?.Select(x => x.CreateAsSoonAsPossibleSchedulingRule()).ToList() ?? [],
                   schedulingParametersPackage.DateRules?.Select(x => x.CreateDateSchedulingRule()).ToList() ?? [],
                   schedulingParametersPackage.TimeRules?.Select(x => x.CreateTimeSchedulingRule()).ToList() ?? [],
                   schedulingParametersPackage.VenueRules?.Select(x => x.CreateVenueSchedulingRule()).ToList() ?? []);

        public static IAvailableDateSchedulingRule CreateAsSoonAsPossibleSchedulingRule(this AsSoonAsPossibleSchedulingRulePackage source)
            => source switch
            {
                IncludeDaysOfWeekAsSoonAsPossibleRulePackage includeDaysOfWeekRule => new IncludeDaysOfWeekRule(includeDaysOfWeekRule.DaysOfWeek?.Split(";").Select(Enum.Parse<DayOfWeek>).ToList() ?? []),
                ExcludeDatesRangeAsSoonAsPossibleRulePackage excludePeriodRule => new ExcludeDatesRangeRule(excludePeriodRule.StartDate, excludePeriodRule.EndDate),
                IncludeTimePeriodsRulePackage includeTimePeriodsRule => new IncludeTimePeriodsRule(includeTimePeriodsRule.TimePeriods?.Select(x => new TimePeriod(x.StartTime, x.EndTime, DateTimeKind.Local)).ToList() ?? []),
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static IDateSchedulingRule CreateDateSchedulingRule(this DateSchedulingRulePackage source)
            => source switch
            {
                IncludeDaysOfWeekRulePackage dateIntervalRule => new IncludeDaysOfWeekRule(dateIntervalRule.DaysOfWeek?.Split(";").Select(Enum.Parse<DayOfWeek>).ToList() ?? []),
                ExcludeDateRulePackage excludeDateRule => new ExcludeDateRule(excludeDateRule.Date),
                ExcludeDatesRangeRulePackage excludeDatesRangeRule => new ExcludeDatesRangeRule(excludeDatesRangeRule.StartDate, excludeDatesRangeRule.EndDate),
                DateIntervalRulePackage dateIntervalRule => new DateIntervalRule(dateIntervalRule.Interval),
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static ITimeSchedulingRule CreateTimeSchedulingRule(this TimeSchedulingRulePackage source)
            => source switch
            {
                TimeOfDayRulePackage timeOfDayRule => new TimeOfDayRule(timeOfDayRule.Day, timeOfDayRule.Time, timeOfDayRule.MatchExceptions?.Select(x => x.CreateTimeSchedulingRule()).OfType<TimeOfMatchNumberRule>() ?? []),
                TimeOfDateRulePackage timeOfDateRule => new TimeOfDateRule(timeOfDateRule.Date, timeOfDateRule.Time, timeOfDateRule.MatchExceptions?.Select(x => x.CreateTimeSchedulingRule()).OfType<TimeOfMatchNumberRule>() ?? []),
                TimeOfMatchNumberRulePackage timeOfMatchNumberRule => new TimeOfMatchNumberRule(timeOfMatchNumberRule.MatchNumber, timeOfMatchNumberRule.Time),
                TimeOfDateRangeRulePackage timeOfDateRangeRule => new TimeOfDatesRangeRule(timeOfDateRangeRule.StartDate, timeOfDateRangeRule.EndDate, timeOfDateRangeRule.Time, timeOfDateRangeRule.MatchExceptions?.Select(x => x.CreateTimeSchedulingRule()).OfType<TimeOfMatchNumberRule>() ?? []),
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static IAvailableVenueSchedulingRule CreateVenueSchedulingRule(this VenueSchedulingRulePackage source)
            => source switch
            {
                HomeStadiumRulePackage => new HomeStadiumRule(),
                AwayStadiumRulePackage => new AwayStadiumRule(),
                NoStadiumRulePackage => new NoStadiumRule(),
                FirstAvailableStadiumRulePackage firstAvailableStadiumRule => new FirstAvailableStadiumRule((UseRotationTime)firstAvailableStadiumRule.UseRotationTime),
                StadiumOfDayRulePackage stadiumOfDayRule => new StadiumOfDayRule(stadiumOfDayRule.Day, stadiumOfDayRule.StadiumId, stadiumOfDayRule.MatchExceptions?.Select(x => x.CreateVenueSchedulingRule()).OfType<StadiumOfMatchNumberRule>() ?? []),
                StadiumOfDateRulePackage stadiumOfDateRule => new StadiumOfDateRule(stadiumOfDateRule.Date, stadiumOfDateRule.StadiumId, stadiumOfDateRule.MatchExceptions?.Select(x => x.CreateVenueSchedulingRule()).OfType<StadiumOfMatchNumberRule>() ?? []),
                StadiumOfMatchNumberRulePackage stadiumOfMatchNumberRule => new StadiumOfMatchNumberRule(stadiumOfMatchNumberRule.MatchNumber, stadiumOfMatchNumberRule.StadiumId),
                StadiumOfDateRangeRulePackage stadiumOfDateRangeRule => new StadiumOfDatesRangeRule(stadiumOfDateRangeRule.StartDate, stadiumOfDateRangeRule.EndDate, stadiumOfDateRangeRule.StadiumId, stadiumOfDateRangeRule.MatchExceptions?.Select(x => x.CreateVenueSchedulingRule()).OfType<StadiumOfMatchNumberRule>() ?? []),
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static Address CreateAddress(this AddressPackage source) => new(source.Street, source.PostalCode, source.City, source.Country.HasValue ? Country.FromValue(source.Country.Value) : null, source.Latitude, source.Longitude);

        public static Stadium CreateStadium(this StadiumPackage source) => new(source.Name, (Ground)source.Ground, id: source.Id)
        {
            Address = source.Address?.CreateAddress()
        };

        public static Team CreateTeam(this TeamPackage source, Stadium? stadium)
        {
            var result = new Team(source.Name, source.ShortName, id: source.Id)
            {
                AwayColor = source.AwayColor,
                HomeColor = source.HomeColor,
                Country = source.Country.HasValue ? Country.FromValue(source.Country.Value) : null,
                Logo = source.Logo,
                Stadium = stadium,
            };
            source.Players?.ForEach(x => result.AddPlayer(x.CreatePlayer(result)));
            source.Staff?.ForEach(x => result.AddManager(x.CreateManager(result)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Player CreatePlayer(this PlayerPackage source, Team team)
        {
            var result = new Player(team, firstName: source.FirstName, lastName: source.LastName, id: source.Id)
            {
                Country = source.Country.HasValue ? Country.FromValue(source.Country.Value) : null,
                Photo = source.Photo,
                Gender = (GenderType)source.Gender,
                LicenseNumber = source.LicenseNumber,
                Email = source.Email,
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        public static Manager CreateManager(this ManagerPackage source, Team team)
        {
            var result = new Manager(team, firstName: source.FirstName, lastName: source.LastName, id: source.Id)
            {
                Country = source.Country.HasValue ? Country.FromValue(source.Country.Value) : null,
                Photo = source.Photo,
                Gender = (GenderType)source.Gender,
                LicenseNumber = source.LicenseNumber,
                Email = source.Email,
            };

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        private static HalfFormat CreateHalfFormat(this HalfFormatPackage source) => new(source.Number, source.Duration, source.HalfTimeDuration);

        private static MatchFormat CreateMatchFormat(this MatchFormatPackage source) => new(source.RegulationTime?.CreateHalfFormat() ?? HalfFormat.Default, source.ExtraTime?.CreateHalfFormat(), source.NumberOfPenaltyShootouts);

        private static RankingRules CreateRankingRules(this RankingRulesPackage source)
            => new(source.Points?.ToDictionary(x => (MatchResultDetailled)x.Result, x => x.Points) ?? [],
                   new RankingComparer(source.Comparers?.Split(';').Select(x => RankingComparer.AllAvailableComparers.GetOrDefault(x)).NotNull() ?? RankingComparer.Default),
                   source.Computers?.Split(';').ToDictionary(x => x, x => RankingRules.CreateComputer(Enum.Parse<DefaultRankingColumn>(x))) ?? RankingRules.DefaultComputers);

        private static Match CreateMatch(this MatchPackage source, IMatchesProvider parent, IEnumerable<Team> teams, IEnumerable<Stadium> stadiums, IEnumerable<Player> players)
        {
            var matchFormatInMatch = source.Format?.CreateMatchFormat() ?? MatchFormat.Default;
            var matchFormatOfParent = parent.ProvideFormat();
            var result = new Match(parent, source.OriginDate, teams.GetById(source.Home!.TeamId), teams.GetById(source.Away!.TeamId), matchFormatInMatch == matchFormatOfParent ? matchFormatOfParent : matchFormatInMatch, source.Id)
            {
                AfterExtraTime = source.AfterExtraTime,
                IsNeutralStadium = source.IsNeutralStadium,
                Stadium = source.StadiumId.HasValue ? stadiums.GetById(source.StadiumId.Value) : null,
            };

            source.Home.Goals?.ForEach(x => result.Home.AddGoal(new Goal((GoalType)x.Type, x.ScorerId.HasValue ? players.GetById(x.ScorerId.Value) : null, x.AssistId.HasValue ? players.GetById(x.AssistId.Value) : null, x.Minute, x.Id)));
            source.Home.Cards?.ForEach(x => result.Home.AddCard(new Card((CardColor)x.Color, x.PlayerId.HasValue ? players.GetById(x.PlayerId.Value) : null, (CardInfraction)x.Infraction, x.Minute, x.Id)));
            source.Home.Shootout?.ForEach(x => result.Home.AddPenaltyShootout(new PenaltyShootout(x.TakerId.HasValue ? players.GetById(x.TakerId.Value) : null, (PenaltyShootoutResult)x.Result, x.Id)));

            if (source.Home.IsWithdrawn)
                result.Home.DoWithdraw();

            source.Away.Goals?.ForEach(x => result.Away.AddGoal(new Goal((GoalType)x.Type, x.ScorerId.HasValue ? players.GetById(x.ScorerId.Value) : null, x.AssistId.HasValue ? players.GetById(x.AssistId.Value) : null, x.Minute, x.Id)));
            source.Away.Cards?.ForEach(x => result.Away.AddCard(new Card((CardColor)x.Color, x.PlayerId.HasValue ? players.GetById(x.PlayerId.Value) : null, (CardInfraction)x.Infraction, x.Minute, x.Id)));
            source.Away.Shootout?.ForEach(x => result.Away.AddPenaltyShootout(new PenaltyShootout(x.TakerId.HasValue ? players.GetById(x.TakerId.Value) : null, (PenaltyShootoutResult)x.Result, x.Id)));

            if (source.Away.IsWithdrawn)
                result.Away.DoWithdraw();

            switch ((MatchState)source.State)
            {
                case MatchState.None:
                    result.Reset();
                    break;
                case MatchState.InProgress:
                    result.Start();
                    break;
                case MatchState.Suspended:
                    result.Suspend();
                    break;
                case MatchState.Cancelled:
                    result.Cancel();
                    break;
                case MatchState.Played:
                    result.Played();
                    break;
                case MatchState.Postponed:
                    result.Postpone(source.PostponedDate);
                    break;
            }

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

        private static Matchday CreateMatchday(this MatchdayPackage source, IMatchdaysProvider parent, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = new Matchday(parent, source.OriginDate, source.Name.OrEmpty(), source.ShortName, source.Id);

            if (source.IsPostponed)
                result.Postpone(source.PostponedDate);

            source.Matches?.ForEach(x => result.AddMatch(x.CreateMatch(result, teams, stadiums, players)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

    }
}
