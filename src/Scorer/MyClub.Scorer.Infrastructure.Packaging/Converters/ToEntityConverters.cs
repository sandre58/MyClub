// Copyright (c) Stéphane ANDRE. All Right Reserved.
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

            // Preferences
            project.Preferences.TreatNoStadiumAsWarning = source.Metadata!.Preferences!.TreatNoStadiumAsWarning;
            project.Preferences.ShowLastMatchFallback = source.Metadata!.Preferences!.ShowLastMatchFallback;
            project.Preferences.ShowNextMatchFallback = source.Metadata!.Preferences!.ShowNextMatchFallback;
            project.Preferences.PeriodForNextMatches = source.Metadata!.Preferences!.PeriodForNextMatches;
            project.Preferences.PeriodForPreviousMatches = source.Metadata!.Preferences!.PeriodForPreviousMatches;

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
                    league.MatchRules = leaguePackage.MatchRules?.CreateMatchRules() ?? MatchRules.Default;
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
            => new(DateOnly.FromDayNumber(schedulingParametersPackage.StartDate),
                   DateOnly.FromDayNumber(schedulingParametersPackage.EndDate),
                   TimeOnly.FromTimeSpan(schedulingParametersPackage.StartTime),
                   schedulingParametersPackage.RotationTime,
                   schedulingParametersPackage.RestTime,
                   schedulingParametersPackage.UseHomeVenue,
                   schedulingParametersPackage.AsSoonAsPossible,
                   schedulingParametersPackage.Interval,
                   schedulingParametersPackage.ScheduleByStage,
                   schedulingParametersPackage.AsSoonAsPossibleRules?.Select(x => x.CreateAsSoonAsPossibleSchedulingRule()).ToList() ?? [],
                   schedulingParametersPackage.DateRules?.Select(x => x.CreateDateSchedulingRule()).ToList() ?? [],
                   schedulingParametersPackage.TimeRules?.Select(x => x.CreateTimeSchedulingRule()).ToList() ?? [],
                   schedulingParametersPackage.VenueRules?.Select(x => x.CreateVenueSchedulingRule()).ToList() ?? []);

        public static IAvailableDateSchedulingRule CreateAsSoonAsPossibleSchedulingRule(this AsSoonAsPossibleSchedulingRulePackage source)
            => source switch
            {
                IncludeDaysOfWeekAsSoonAsPossibleRulePackage includeDaysOfWeekRule => new IncludeDaysOfWeekRule(includeDaysOfWeekRule.DaysOfWeek?.Split(";").Select(Enum.Parse<DayOfWeek>).ToList() ?? []),
                ExcludeDatesRangeAsSoonAsPossibleRulePackage excludePeriodRule => new ExcludeDatesRangeRule(DateOnly.FromDayNumber(excludePeriodRule.StartDate), DateOnly.FromDayNumber(excludePeriodRule.EndDate)),
                IncludeTimePeriodsRulePackage includeTimePeriodsRule => new IncludeTimePeriodsRule(includeTimePeriodsRule.TimePeriods?.Select(x => new TimePeriod(TimeOnly.FromTimeSpan(x.StartTime), TimeOnly.FromTimeSpan(x.EndTime))).ToList() ?? []),
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static IDateSchedulingRule CreateDateSchedulingRule(this DateSchedulingRulePackage source)
            => source switch
            {
                IncludeDaysOfWeekRulePackage dateIntervalRule => new IncludeDaysOfWeekRule(dateIntervalRule.DaysOfWeek?.Split(";").Select(Enum.Parse<DayOfWeek>).ToList() ?? []),
                ExcludeDateRulePackage excludeDateRule => new ExcludeDateRule(DateOnly.FromDayNumber(excludeDateRule.Date)),
                ExcludeDatesRangeRulePackage excludeDatesRangeRule => new ExcludeDatesRangeRule(DateOnly.FromDayNumber(excludeDatesRangeRule.StartDate), DateOnly.FromDayNumber(excludeDatesRangeRule.EndDate)),
                DateIntervalRulePackage dateIntervalRule => new DateIntervalRule(dateIntervalRule.Interval),
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static ITimeSchedulingRule CreateTimeSchedulingRule(this TimeSchedulingRulePackage source)
            => source switch
            {
                TimeOfDayRulePackage timeOfDayRule => new TimeOfDayRule(timeOfDayRule.Day, TimeOnly.FromTimeSpan(timeOfDayRule.Time), timeOfDayRule.Exceptions?.Select(x => x.CreateTimeSchedulingRule()).OfType<TimeOfIndexRule>() ?? []),
                TimeOfDateRulePackage timeOfDateRule => new TimeOfDateRule(DateOnly.FromDayNumber(timeOfDateRule.Date), TimeOnly.FromTimeSpan(timeOfDateRule.Time), timeOfDateRule.Exceptions?.Select(x => x.CreateTimeSchedulingRule()).OfType<TimeOfIndexRule>() ?? []),
                TimeOfIndexRulePackage timeOfMatchNumberRule => new TimeOfIndexRule(timeOfMatchNumberRule.Index, TimeOnly.FromTimeSpan(timeOfMatchNumberRule.Time)),
                TimeOfDateRangeRulePackage timeOfDateRangeRule => new TimeOfDatesRangeRule(DateOnly.FromDayNumber(timeOfDateRangeRule.StartDate), DateOnly.FromDayNumber(timeOfDateRangeRule.EndDate), TimeOnly.FromTimeSpan(timeOfDateRangeRule.Time), timeOfDateRangeRule.Exceptions?.Select(x => x.CreateTimeSchedulingRule()).OfType<TimeOfIndexRule>() ?? []),
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static IAvailableVenueSchedulingRule CreateVenueSchedulingRule(this VenueSchedulingRulePackage source)
            => source switch
            {
                HomeStadiumRulePackage => new HomeStadiumRule(),
                AwayStadiumRulePackage => new AwayStadiumRule(),
                NoStadiumRulePackage => new NoStadiumRule(),
                FirstAvailableStadiumRulePackage firstAvailableStadiumRule => new FirstAvailableStadiumRule((UseRotationTime)firstAvailableStadiumRule.UseRotationTime),
                StadiumOfDayRulePackage stadiumOfDayRule => new StadiumOfDayRule(stadiumOfDayRule.Day, stadiumOfDayRule.StadiumId),
                StadiumOfDateRulePackage stadiumOfDateRule => new StadiumOfDateRule(DateOnly.FromDayNumber(stadiumOfDateRule.Date), stadiumOfDateRule.StadiumId),
                StadiumOfDateRangeRulePackage stadiumOfDateRangeRule => new StadiumOfDatesRangeRule(DateOnly.FromDayNumber(stadiumOfDateRangeRule.StartDate), DateOnly.FromDayNumber(stadiumOfDateRangeRule.EndDate), stadiumOfDateRangeRule.StadiumId),
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

        private static MatchRules CreateMatchRules(this MatchRulesPackage source) => new(source.AllowedCards?.Split(";").Select(Enum.Parse<CardColor>) ?? []);

        private static RankingRules CreateRankingRules(this RankingRulesPackage source)
            => new(source.Points?.ToDictionary(x => (ExtendedResult)x.Result, x => x.Points) ?? [],
                   new RankingComparer(source.Comparers?.Split(';').Select(x => RankingComparer.AllAvailableComparers.GetOrDefault(x)).NotNull() ?? RankingComparer.Default),
                   source.Computers?.Split(';').ToDictionary(x => x, x => RankingRules.CreateComputer(Enum.Parse<DefaultRankingColumn>(x))) ?? RankingRules.DefaultComputers);

        private static MatchOfMatchday CreateMatchOfMatchday(this MatchPackage source, Matchday stage, IEnumerable<Team> teams, IEnumerable<Stadium> stadiums, IEnumerable<Player> players)
        {
            var result = new MatchOfMatchday(stage,
                                             source.OriginDate,
                                             teams.GetById(source.Home!.TeamId),
                                             teams.GetById(source.Away!.TeamId),
                                             source.Id);

            result.Update(source, stadiums, players);

            return result;
        }

        private static void Update(this MatchOfMatchday match, MatchPackage source, IEnumerable<Stadium> stadiums, IEnumerable<Player> players)
        {
            match.AfterExtraTime = source.AfterExtraTime;
            match.IsNeutralStadium = source.IsNeutralStadium;
            match.Stadium = source.StadiumId.HasValue ? stadiums.GetById(source.StadiumId.Value) : null;
            switch ((MatchState)source.State)
            {
                case MatchState.None:
                    match.Reset();
                    break;
                case MatchState.InProgress:
                    match.Start();
                    break;
                case MatchState.Suspended:
                    match.Suspend();
                    break;
                case MatchState.Cancelled:
                    match.Cancel();
                    break;
                case MatchState.Played:
                    match.Played();
                    break;
                case MatchState.Postponed:
                    match.Postpone(source.PostponedDate);
                    break;
            }

            if (source.Home is null || source.Away is null)
                return;

            if (source.Home.IsWithdrawn)
                match.Home?.DoWithdraw();

            if (source.Away.IsWithdrawn)
                match.Away?.DoWithdraw();

            source.Home.Goals?.ForEach(x => match.Home?.AddGoal(new Goal((GoalType)x.Type, x.ScorerId.HasValue ? players.GetById(x.ScorerId.Value) : null, x.AssistId.HasValue ? players.GetById(x.AssistId.Value) : null, x.Minute, x.Id)));
            source.Home.Cards?.ForEach(x => match.Home?.AddCard(new Card((CardColor)x.Color, x.PlayerId.HasValue ? players.GetById(x.PlayerId.Value) : null, (CardInfraction)x.Infraction, x.Minute, x.Id)));
            source.Home.Shootout?.ForEach(x => match.Home?.AddPenaltyShootout(new PenaltyShootout(x.TakerId.HasValue ? players.GetById(x.TakerId.Value) : null, (PenaltyShootoutResult)x.Result, x.Id)));

            source.Away.Goals?.ForEach(x => match.Away?.AddGoal(new Goal((GoalType)x.Type, x.ScorerId.HasValue ? players.GetById(x.ScorerId.Value) : null, x.AssistId.HasValue ? players.GetById(x.AssistId.Value) : null, x.Minute, x.Id)));
            source.Away.Cards?.ForEach(x => match.Away?.AddCard(new Card((CardColor)x.Color, x.PlayerId.HasValue ? players.GetById(x.PlayerId.Value) : null, (CardInfraction)x.Infraction, x.Minute, x.Id)));
            source.Away.Shootout?.ForEach(x => match.Away?.AddPenaltyShootout(new PenaltyShootout(x.TakerId.HasValue ? players.GetById(x.TakerId.Value) : null, (PenaltyShootoutResult)x.Result, x.Id)));

            match.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            match.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);
        }

        private static Matchday CreateMatchday(this MatchdayPackage source, IMatchdaysStage stage, IList<Team> teams, IList<Stadium> stadiums, IList<Player> players)
        {
            var result = new Matchday(stage, source.OriginDate, source.Name.OrEmpty(), source.ShortName, source.Id);

            if (source.IsPostponed)
                result.Postpone(source.PostponedDate);

            source.Matches?.ForEach(x => result.AddMatch(x.CreateMatchOfMatchday(result, teams, stadiums, players)));

            result.MarkedAsCreated(source.CreatedAt, source.CreatedBy);
            result.MarkedAsModified(source.ModifiedAt, source.ModifiedBy);

            return result;
        }

    }
}
