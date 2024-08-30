// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
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
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Infrastructure.Packaging.Converters
{
    public static class ToPackageConverters
    {
        public static ProjectPackage ToPackage(this IProject source)
            => new()
            {
                Metadata = new MetadataPackage
                {
                    Id = source.Id,
                    Type = (int)source.Type,
                    Name = source.Name,
                    Image = source.Image,
                    Preferences = source.Preferences.ToPackage(),
                    CreatedAt = source.CreatedAt,
                    CreatedBy = source.CreatedBy,
                    ModifiedAt = source.ModifiedAt,
                    ModifiedBy = source.ModifiedBy
                },
                Competition = source.Type switch
                {
                    CompetitionType.League => new LeaguePackage
                    {
                        SchedulingParameters = source.Competition.ProvideSchedulingParameters().ToPackage(),
                        Labels = ((League)source.Competition).Labels.Select(x => new RankLabelPackage
                        {
                            Color = x.Value.Color,
                            Description = x.Value.Description,
                            Name = x.Value.Name,
                            ShortName = x.Value.ShortName,
                            EndRank = x.Key.Max ?? 1,
                            StartRank = x.Key.Min ?? 1
                        }).ToList(),
                        Matchdays = ((League)source.Competition).Matchdays.Select(x => x.ToPackage()).ToList(),
                        MatchFormat = ((League)source.Competition).MatchFormat.ToPackage(),
                        Penalties = ((League)source.Competition).GetPenaltyPoints().Select(x => new PenaltyPackage() { TeamId = x.Key.Id, Penalty = x.Value }).ToList(),
                        RankingRules = ((League)source.Competition).RankingRules.ToPackage(),
                    },
                    CompetitionType.Cup => new CupPackage
                    {
                    },
                    CompetitionType.Tournament => new TournamentPackage
                    {
                    },
                    _ => throw new InvalidOperationException("Unknown competition type")
                },
                Stadiums = new StadiumsPackage(source.Stadiums.Select(x => x.ToPackage())),
                Teams = new TeamsPackage(source.Teams.Select(x => x.ToPackage())),
            };

        public static ProjectPreferencesPackage ToPackage(this ProjectPreferences source) => new()
        {
            TreatNoStadiumAsWarning = source.TreatNoStadiumAsWarning
        };

        public static AddressPackage ToPackage(this Address source) => new()
        {
            City = source.City,
            Country = source.Country is not null ? (int)source.Country : null,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            PostalCode = source.PostalCode,
            Street = source.Street
        };

        public static StadiumPackage ToPackage(this Stadium source)
        => new()
        {
            Name = source.Name,
            Ground = (int)source.Ground,
            Address = source.Address?.ToPackage(),
            Id = source.Id
        };

        public static TeamPackage ToPackage(this Team source)
        => new()
        {
            ShortName = source.ShortName,
            Name = source.Name,
            Country = source.Country is not null ? (int)source.Country : null,
            Logo = source.Logo,
            AwayColor = source.AwayColor,
            HomeColor = source.HomeColor,
            StadiumId = source.Stadium?.Id,
            Players = source.Players.Select(x => x.ToPackage()).ToList(),
            Staff = source.Staff.Select(x => x.ToPackage()).ToList(),
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy,
            Id = source.Id
        };

        public static PlayerPackage ToPackage(this Player source)
        => new()
        {
            LastName = source.LastName,
            FirstName = source.FirstName,
            Photo = source.Photo,
            Gender = (int)source.Gender,
            LicenseNumber = source.LicenseNumber,
            Email = source.Email,
            Country = source.Country is not null ? (int)source.Country : null,
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy,
            Id = source.Id
        };

        public static ManagerPackage ToPackage(this Manager source)
        => new()
        {
            LastName = source.LastName,
            FirstName = source.FirstName,
            Photo = source.Photo,
            Gender = (int)source.Gender,
            LicenseNumber = source.LicenseNumber,
            Email = source.Email,
            Country = source.Country is not null ? (int)source.Country : null,
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
            ModifiedAt = source.ModifiedAt,
            ModifiedBy = source.ModifiedBy,
            Id = source.Id
        };

        public static MatchPackage ToPackage(this Match source)
            => new()
            {
                Id = source.Id,
                AfterExtraTime = source.AfterExtraTime,
                Format = source.Format.ToPackage(),
                IsNeutralStadium = source.IsNeutralStadium,
                State = (int)source.State,
                OriginDate = source.OriginDate,
                StadiumId = source.Stadium?.Id,
                Away = new MatchOpponentPackage
                {
                    TeamId = source.AwayTeam.Id,
                    IsWithdrawn = source.Away.IsWithdrawn,
                    Goals = source.Away.GetGoals().Select(x => new GoalPackage { Id = x.Id, AssistId = x.Assist?.Id, Minute = x.Minute, ScorerId = x.Scorer?.Id, Type = (int)x.Type }).ToList(),
                    Cards = source.Away.GetCards().Select(x => new CardPackage { Id = x.Id, PlayerId = x.Player?.Id, Minute = x.Minute, Color = (int)x.Color, Description = x.Description, Infraction = (int)x.Infraction }).ToList(),
                    Shootout = source.Away.Shootout.Select(x => new PenaltyShootoutPackage { Id = x.Id, TakerId = x.Taker?.Id, Result = (int)x.Result }).ToList(),
                },
                Home = new MatchOpponentPackage
                {
                    TeamId = source.HomeTeam.Id,
                    IsWithdrawn = source.Home.IsWithdrawn,
                    Goals = source.Home.GetGoals().Select(x => new GoalPackage { Id = x.Id, AssistId = x.Assist?.Id, Minute = x.Minute, ScorerId = x.Scorer?.Id, Type = (int)x.Type }).ToList(),
                    Cards = source.Home.GetCards().Select(x => new CardPackage { Id = x.Id, PlayerId = x.Player?.Id, Minute = x.Minute, Color = (int)x.Color, Description = x.Description, Infraction = (int)x.Infraction }).ToList(),
                    Shootout = source.Home.Shootout.Select(x => new PenaltyShootoutPackage { Id = x.Id, TakerId = x.Taker?.Id, Result = (int)x.Result }).ToList(),
                },
                PostponedDate = source.State == MatchState.Postponed ? source.Date : null,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
            };

        public static MatchdayPackage ToPackage(this Matchday source)
            => new()
            {
                Id = source.Id,
                IsPostponed = source.IsPostponed,
                Name = source.Name,
                OriginDate = source.OriginDate,
                ShortName = source.ShortName,
                PostponedDate = source.IsPostponed ? source.Date : null,
                Matches = source.Matches.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
            };

        public static HalfFormatPackage ToPackage(this HalfFormat source)
            => new()
            {
                Duration = source.Duration,
                Number = source.Number,
                HalfTimeDuration = source.HalfTimeDuration,
            };

        public static MatchFormatPackage ToPackage(this MatchFormat source)
            => new()
            {
                ExtraTime = source.ExtraTime?.ToPackage(),
                RegulationTime = source.RegulationTime.ToPackage(),
                NumberOfPenaltyShootouts = source.NumberOfPenaltyShootouts
            };

        public static RankingRulesPackage ToPackage(this RankingRules source)
            => new()
            {
                Points = source.PointsNumberByResult.Select(x => new PointsNumberByResultPackage
                {
                    Points = x.Value,
                    Result = (int)x.Key
                }).ToList(),
                Comparers = string.Join(";", source.Comparer.Select(x => x.GetType().Name).ToList()),
                Computers = string.Join(";", source.Computers.Select(x => x.Key).ToList()),
            };

        public static SchedulingParametersPackage ToPackage(this SchedulingParameters source)
            => new()
            {
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                StartTime = source.StartTime,
                RestTime = source.RotationTime,
                RotationTime = source.RestTime,
                UseHomeVenue = source.UseHomeVenue,
                AsSoonAsPossible = source.AsSoonAsPossible,
                Interval = source.Interval,
                ScheduleByParent = source.ScheduleByParent,
                AsSoonAsPossibleRules = source.AsSoonAsPossibleRules.Select(x => x.ToPackage()).ToList(),
                DateRules = source.DateRules.Select(x => x.ToPackage()).ToList(),
                TimeRules = source.TimeRules.Select(x => x.ToPackage()).ToList(),
                VenueRules = source.VenueRules.Select(x => x.ToPackage()).ToList()
            };

        public static AsSoonAsPossibleSchedulingRulePackage ToPackage(this IAvailableDateSchedulingRule source)
            => source switch
            {
                IncludeDaysOfWeekRule includeDaysOfWeekRule => new IncludeDaysOfWeekAsSoonAsPossibleRulePackage { DaysOfWeek = string.Join(";", includeDaysOfWeekRule.DaysOfWeek.Select(x => x.ToString()).ToList()) },
                ExcludeDatesRangeRule excludePeriodRule => new ExcludeDatesRangeAsSoonAsPossibleRulePackage { StartDate = excludePeriodRule.StartDate, EndDate = excludePeriodRule.EndDate },
                IncludeTimePeriodsRule includeTimePeriodsRule => new IncludeTimePeriodsRulePackage { TimePeriods = includeTimePeriodsRule.Periods.Select(x => new TimePeriodPackage() { StartTime = x.Start, EndTime = x.End }).ToList() },
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static DateSchedulingRulePackage ToPackage(this IDateSchedulingRule source)
            => source switch
            {
                IncludeDaysOfWeekRule dayOfWeeksRule => new IncludeDaysOfWeekRulePackage { DaysOfWeek = string.Join(";", dayOfWeeksRule.DaysOfWeek.Select(x => x.ToString()).ToList()) },
                ExcludeDateRule excludeDateRule => new ExcludeDateRulePackage { Date = excludeDateRule.Date },
                ExcludeDatesRangeRule excludeDatesRangeRule => new ExcludeDatesRangeRulePackage { StartDate = excludeDatesRangeRule.StartDate, EndDate = excludeDatesRangeRule.EndDate },
                DateIntervalRule dateIntervalRule => new DateIntervalRulePackage { Interval = dateIntervalRule.Interval },
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static TimeSchedulingRulePackage ToPackage(this ITimeSchedulingRule source)
            => source switch
            {
                TimeOfDayRule timeOfDayRule => new TimeOfDayRulePackage { Day = timeOfDayRule.Day, Time = timeOfDayRule.Time, MatchExceptions = timeOfDayRule.MatchExceptions.Select(x => x.ToPackage()).OfType<TimeOfMatchNumberRulePackage>().ToList() },
                TimeOfDateRule timeOfDateRule => new TimeOfDateRulePackage { Date = timeOfDateRule.Date, Time = timeOfDateRule.Time, MatchExceptions = timeOfDateRule.MatchExceptions.Select(x => x.ToPackage()).OfType<TimeOfMatchNumberRulePackage>().ToList() },
                TimeOfMatchNumberRule timeOfMatchNumberRule => new TimeOfMatchNumberRulePackage { MatchNumber = timeOfMatchNumberRule.MatchNumber, Time = timeOfMatchNumberRule.Time },
                TimeOfDatesRangeRule timeOfDateRangeRule => new TimeOfDateRangeRulePackage { StartDate = timeOfDateRangeRule.StartDate, EndDate = timeOfDateRangeRule.EndDate, Time = timeOfDateRangeRule.Time, MatchExceptions = timeOfDateRangeRule.MatchExceptions.Select(x => x.ToPackage()).OfType<TimeOfMatchNumberRulePackage>().ToList() },
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };

        public static VenueSchedulingRulePackage ToPackage(this IAvailableVenueSchedulingRule source)
            => source switch
            {
                HomeStadiumRule => new HomeStadiumRulePackage(),
                AwayStadiumRule => new AwayStadiumRulePackage(),
                NoStadiumRule => new NoStadiumRulePackage(),
                FirstAvailableStadiumRule firstAvailableStadiumRule => new FirstAvailableStadiumRulePackage() { UseRotationTime = (int)firstAvailableStadiumRule.UseRotationTime },
                StadiumOfDayRule stadiumOfDayRule => new StadiumOfDayRulePackage { Day = stadiumOfDayRule.Day, StadiumId = stadiumOfDayRule.StadiumId, MatchExceptions = stadiumOfDayRule.MatchExceptions.Select(x => x.ToPackage()).OfType<StadiumOfMatchNumberRulePackage>().ToList() },
                StadiumOfDateRule stadiumOfDateRule => new StadiumOfDateRulePackage { Date = stadiumOfDateRule.Date, StadiumId = stadiumOfDateRule.StadiumId, MatchExceptions = stadiumOfDateRule.MatchExceptions.Select(x => x.ToPackage()).OfType<StadiumOfMatchNumberRulePackage>().ToList() },
                StadiumOfMatchNumberRule stadiumOfMatchNumberRule => new StadiumOfMatchNumberRulePackage { MatchNumber = stadiumOfMatchNumberRule.MatchNumber, StadiumId = stadiumOfMatchNumberRule.StadiumId },
                StadiumOfDatesRangeRule stadiumOfDateRangeRule => new StadiumOfDateRangeRulePackage { StartDate = stadiumOfDateRangeRule.StartDate, EndDate = stadiumOfDateRangeRule.EndDate, StadiumId = stadiumOfDateRangeRule.StadiumId, MatchExceptions = stadiumOfDateRangeRule.MatchExceptions.Select(x => x.ToPackage()).OfType<StadiumOfMatchNumberRulePackage>().ToList() },
                _ => throw new InvalidOperationException($"{source.GetType()} cannot be converted in package"),
            };
    }
}
