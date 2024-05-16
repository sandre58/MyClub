// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Infrastructure.Packaging.Models;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Infrastructure.Packaging.Converters
{
    public static class ToPackageConverters
    {
        public static ProjectPackage ToPackage(this Project source)
            => new()
            {
                Metadata = new MetadataPackage
                {
                    Id = source.Id,
                    Name = source.Name,
                    Image = source.Image,
                    Category = source.Category,
                    ClubId = source.Club.Id,
                    Color = source.Color,
                    SeasonId = source.Season.Id,
                    TeamId = source.MainTeam?.Id,
                    Preferences = new ProjectPreferencesPackage
                    {
                        TrainingDuration = source.Preferences.TrainingDuration,
                        TrainingStartTime = source.Preferences.TrainingStartTime
                    },
                    CreatedAt = source.CreatedAt,
                    CreatedBy = source.CreatedBy,
                    ModifiedAt = source.ModifiedAt,
                    ModifiedBy = source.ModifiedBy
                },
                Seasons = new SeasonsPackage([source.Season.ToPackage()]),
                Squads = new SquadsPackage([
                            new SquadPackage
                            {
                                Players = source.Players.Select(x => x.ToPackage()).ToList()
                            }
                ]),
                Players = new PlayersPackage(source.Players.Select(x => x.Player.ToPackage())),
                TrainingSessions = new TrainingSessionsPackage(source.TrainingSessions.Select(x => x.ToPackage())),
                SendedMails = new SendedMailsPackage(source.SendedMails.Select(x => x.ToPackage())),
                Holidays = new HolidaysPackage(source.Holidays.Select(x => x.ToPackage())),
                Cycles = new CyclesPackage(source.Cycles.Select(x => x.ToPackage())),
                Tactics = new TacticsPackage(source.Tactics.Select(x => x.ToPackage())),
                Stadiums = new StadiumsPackage(source.Competitions.SelectMany(x => x.Teams).Select(x => x.Stadium.Value)
                                              .Concat(source.Competitions.SelectMany(x => x.GetAllMatches()).Select(x => x.Stadium))
                                              .Concat(source.Club.Teams.Select(x => x.Stadium.Value))
                                              .NotNull()
                                              .Distinct()
                                              .Select(x => x.ToPackage())),
                Clubs = new ClubsPackage(new[] { source.Club }.Concat(source.Competitions.SelectMany(x => x.Teams).Select(x => x.Club)).Distinct().Select(x => x.ToPackage())),
                Competitions = new CompetitionsPackage
                {
                    Cups = new CupsPackage(source.Competitions.OfType<CupSeason>().Select(x => x.Competition).Distinct().OfType<Cup>().Select(x => x.ToPackage())),
                    Friendlies = new FriendliesPackage(source.Competitions.OfType<FriendlySeason>().Select(x => x.Competition).Distinct().OfType<Friendly>().Select(x => x.ToPackage())),
                    Leagues = new LeaguesPackage(source.Competitions.OfType<LeagueSeason>().Select(x => x.Competition).Distinct().OfType<League>().Select(x => x.ToPackage()))
                },
            };

        public static SeasonPackage ToPackage(this Season source)
                => new()
                {
                    Label = source.Label,
                    Code = source.Code,
                    Description = source.Description,
                    StartDate = source.Period.Start,
                    EndDate = source.Period.End,
                    Id = source.Id,
                    Order = source.Order,
                    CreatedAt = source.CreatedAt,
                    CreatedBy = source.CreatedBy,
                    ModifiedAt = source.ModifiedAt,
                    ModifiedBy = source.ModifiedBy,
                };

        public static ContactPackage ToPackage(this Email source)
            => new()
            {
                Label = source.Label,
                Default = source.Default,
                Value = source.Value
            };

        public static ContactPackage ToPackage(this Phone source)
            => new()
            {
                Label = source.Label,
                Default = source.Default,
                Value = source.Value
            };

        public static InjuryPackage ToPackage(this Injury source)
            => new()
            {
                EndDate = source.Period.End,
                Date = source.Period.Start,
                Condition = source.Condition,
                Severity = (int)source.Severity,
                Type = (int)source.Type,
                Category = (int)source.Category,
                Description = source.Description,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static AbsencePackage ToPackage(this Absence source)
            => new()
            {
                StartDate = source.Period.Start,
                EndDate = source.Period.End,
                Type = (int)source.Type,
                Label = source.Label,
                Id = source.Id,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
            };

        public static RatedPositionPackage ToPackage(this RatedPosition source)
            => new()
            {
                Position = source.Position,
                Rating = (int)source.Rating,
                IsNatural = source.IsNatural,
                Id = source.Id
            };

        public static PlayerPackage ToPackage(this Player source)
            => new()
            {
                Laterality = (int)source.Laterality,
                Height = source.Height,
                Weight = source.Weight,
                Injuries = source.Injuries.Select(x => x.ToPackage()).ToList(),
                Positions = source.Positions.Select(x => x.ToPackage()).ToList(),
                Absences = source.Absences.Select(x => x.ToPackage()).ToList(),
                LastName = source.LastName,
                FirstName = source.FirstName,
                Birthdate = source.Birthdate,
                PlaceOfBirth = source.PlaceOfBirth,
                Address = source.Address?.ToPackage(),
                Photo = source.Photo,
                Gender = (int)source.Gender,
                LicenseNumber = source.LicenseNumber,
                Description = source.Description,
                Country = source.Country is not null ? (int)source.Country : null,
                Category = source.Category is not null ? (int)source.Category : null,
                Phones = source.Phones.Select(x => x.ToPackage()).ToList(),
                Emails = source.Emails.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static AddressPackage ToPackage(this Address address) => new()
        {
            City = address.City,
            Country = address.Country is not null ? (int)address.Country : null,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            PostalCode = address.PostalCode,
            Street = address.Street
        };

        public static TacticPositionPackage ToPackage(this TacticPosition source)
            => new()
            {
                Position = source.Position,
                Number = source.Number,
                Instructions = string.Join(";", source.Instructions),
                OffsetX = source.OffsetX,
                OffsetY = source.OffsetY,
                Id = source.Id
            };

        public static TacticPackage ToPackage(this Tactic source)
            => new()
            {
                Label = source.Label,
                Description = source.Description,
                Code = source.Code,
                Order = source.Order,
                Instructions = string.Join(";", source.Instructions),
                Positions = source.Positions.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static SquadPlayerPackage ToPackage(this SquadPlayer source)
            => new()
            {
                PlayerId = source.Player.Id,
                TeamId = source.Team?.Id,
                Category = source.Category is not null ? (int)source.Category : null,
                ShoesSize = source.ShoesSize,
                LicenseState = (int)source.LicenseState,
                IsMutation = source.IsMutation,
                Number = source.Number,
                Positions = source.Positions.Select(x => x.ToPackage()).ToList(),
                FromDate = source.FromDate,
                Size = source.Size,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static SquadPackage ToPackage(this Squad source)
            => new()
            {
                Category = source.Category,
                ClubId = source.Club.Id,
                Code = source.Code,
                Description = source.Description,
                Label = source.Label,
                Players = source.Players.Select(x => x.ToPackage()).ToList(),
                SeasonId = source.Season.Id,
                Order = source.Order,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static ClubPackage ToPackage(this Club source)
            => new()
            {
                ShortName = source.ShortName,
                Name = source.Name,
                Country = source.Country is not null ? (int)source.Country : null,
                Logo = source.Logo,
                AwayColor = source.AwayColor,
                HomeColor = source.HomeColor,
                StadiumId = source.Stadium?.Id,
                Teams = source.Teams.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static TeamPackage ToPackage(this Team source)
            => new()
            {
                Category = source.Category,
                Order = source.Order,
                ShortName = source.ShortName,
                Name = source.Name,
                AwayColor = source.AwayColor.OverrideValue,
                HomeColor = source.HomeColor.OverrideValue,
                StadiumId = source.Stadium.OverrideValue?.Id,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static HalfFormatPackage ToPackage(this HalfFormat source)
            => new()
            {
                Duration = source.Duration,
                Number = source.Number
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
                PointsByGamesWon = source.PointsByGamesWon,
                PointsByGamesDrawn = source.PointsByGamesDrawn,
                PointsByGamesLost = source.PointsByGamesLost,
                SortingColumns = string.Join(";", source.SortingColumns.Select(x => (int)x)),
                Labels = source.Labels.Select(x => new RankLabelPackage { StartRank = x.Key.Min ?? 1, EndRank = x.Key.Max ?? 1, Color = x.Value.Color, Description = x.Value.Description, Name = x.Value.Name, ShortName = x.Value.ShortName }).ToList()
            };

        public static MatchPackage ToPackage(this Match source)
            => new()
            {
                Id = source.Id,
                AfterExtraTime = source.AfterExtraTime,
                Format = source.Format.ToPackage(),
                NeutralVenue = source.NeutralVenue,
                State = (int)source.State,
                OriginDate = source.OriginDate,
                StadiumId = source.Stadium?.Id,
                Away = new MatchOpponentPackage
                {
                    TeamId = source.AwayTeam.Id,
                    IsWithdrawn = source.Away.IsWithdrawn,
                    PenaltyPoints = source.Away.PenaltyPoints,
                    Goals = source.Away.GetGoals().Select(x => new GoalPackage { Id = x.Id, AssistId = x.Assist?.Id, Minute = x.Minute, ScorerId = x.Scorer?.Id, Type = (int)x.Type }).ToList(),
                    Cards = source.Away.GetCards().Select(x => new CardPackage { Id = x.Id, PlayerId = x.Player?.Id, Minute = x.Minute, Color = (int)x.Color, Description = x.Description, Infraction = (int)x.Infraction }).ToList(),
                    Shootout = source.Away.Shootout.Select(x => new PenaltyShootoutPackage { Id = x.Id, TakerId = x.Taker?.Id, Result = (int)x.Result }).ToList(),
                },
                Home = new MatchOpponentPackage
                {
                    TeamId = source.HomeTeam.Id,
                    IsWithdrawn = source.Home.IsWithdrawn,
                    PenaltyPoints = source.Home.PenaltyPoints,
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
                MatchFormat = source.MatchFormat.ToPackage(),
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

        public static KnockoutPackage ToPackage(this Knockout source)
            => new()
            {
                Id = source.Id,
                IsPostponed = source.IsPostponed,
                MatchFormat = source.Rules.MatchFormat.ToPackage(),
                MatchTime = source.Rules.MatchTime,
                Name = source.Name,
                OriginDate = source.OriginDate,
                ShortName = source.ShortName,
                PostponedDate = source.IsPostponed ? source.Date : null,
                TeamIds = string.Join(";", source.Teams.Select(x => x.Id).ToList()),
                Matches = source.Matches.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
            };

        public static GroupStagePackage ToPackage(this GroupStage source)
            => new()
            {
                ShortName = source.ShortName,
                Name = source.Name,
                MatchTime = source.Rules.MatchTime,
                MatchFormat = source.Rules.MatchFormat.ToPackage(),
                StartDate = source.Period.Start,
                EndDate = source.Period.End,
                TeamIds = string.Join(";", source.Teams.Select(x => x.Id).ToList()),
                RankingRules = source.Rules.RankingRules.ToPackage(),
                Groups = source.Groups.Select(x => x.ToPackage()).ToList(),
                Matchdays = source.Matchdays.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static GroupPackage ToPackage(this Group source)
            => new()
            {
                ShortName = source.ShortName,
                Name = source.Name,
                TeamIds = string.Join(";", source.Teams.Select(x => x.Id).ToList()),
                Penalties = source.Penalties?.Select(x => new PenaltyPackage() { TeamId = x.Key.Id, Penalty = x.Value }).ToList(),
                Order = source.Order,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static FriendlyPackage ToPackage(this Friendly source)
            => new()
            {
                ShortName = source.ShortName,
                Name = source.Name,
                Logo = source.Logo,
                MatchTime = source.Rules.MatchTime,
                MatchFormat = source.Rules.MatchFormat.ToPackage(),
                Seasons = source.Seasons.Select(x => x.ToPackage()).ToList(),
                Category = source.Category,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static FriendlySeasonPackage ToPackage(this FriendlySeason source)
            => new()
            {
                SeasonId = source.Season.Id,
                MatchTime = source.Rules.MatchTime,
                StartDate = source.Period.Start,
                EndDate = source.Period.End,
                TeamIds = string.Join(";", source.Teams.Select(x => x.Id).ToList()),
                MatchFormat = source.Rules.MatchFormat.ToPackage(),
                Matches = source.Matches.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static CupPackage ToPackage(this Cup source)
            => new()
            {
                Category = source.Category,
                MatchTime = source.Rules.MatchTime,
                ShortName = source.ShortName,
                Name = source.Name,
                Logo = source.Logo,
                MatchFormat = source.Rules.MatchFormat.ToPackage(),
                Seasons = source.Seasons.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static CupSeasonPackage ToPackage(this CupSeason source)
            => new()
            {
                MatchTime = source.Rules.MatchTime,
                SeasonId = source.Season.Id,
                MatchFormat = source.Rules.MatchFormat.ToPackage(),
                StartDate = source.Period.Start,
                EndDate = source.Period.End,
                TeamIds = string.Join(";", source.Teams.Select(x => x.Id).ToList()),
                Rounds = source.Rounds.Select(x =>
                {
                    RoundPackage item = x switch
                    {
                        Knockout knockout => knockout.ToPackage(),
                        GroupStage groupStage => groupStage.ToPackage(),
                        _ => throw new InvalidOperationException("Round type is unknown"),
                    };
                    return item;
                }).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static LeaguePackage ToPackage(this League source)
            => new()
            {
                MatchTime = source.Rules.MatchTime,
                Category = source.Category,
                ShortName = source.ShortName,
                Name = source.Name,
                Logo = source.Logo,
                MatchFormat = source.Rules.MatchFormat.ToPackage(),
                RankingRules = source.Rules.RankingRules.ToPackage(),
                Seasons = source.Seasons.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static SeasonLeaguePackage ToPackage(this LeagueSeason source)
            => new()
            {
                MatchTime = source.Rules.MatchTime,
                SeasonId = source.Season.Id,
                MatchFormat = source.Rules.MatchFormat.ToPackage(),
                StartDate = source.Period.Start,
                EndDate = source.Period.End,
                TeamIds = string.Join(";", source.Teams.Select(x => x.Id).ToList()),
                Penalties = source.Penalties?.Select(x => new PenaltyPackage() { TeamId = x.Key.Id, Penalty = x.Value }).ToList(),
                RankingRules = source.Rules.RankingRules.ToPackage(),
                Matchdays = source.Matchdays.Select(x => x.ToPackage()).ToList(),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static HolidaysItemPackage ToPackage(this Holidays source)
            => new()
            {
                StartDate = source.Period.Start,
                EndDate = source.Period.End,
                Label = source.Label,
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static CyclePackage ToPackage(this Cycle source)
            => new()
            {
                EndDate = source.Period.End,
                StartDate = source.Period.Start,
                Label = source.Label,
                Color = source.Color,
                TechnicalGoals = string.Join(";", source.TechnicalGoals),
                TacticalGoals = string.Join(";", source.TacticalGoals),
                PhysicalGoals = string.Join(";", source.PhysicalGoals),
                MentalGoals = string.Join(";", source.MentalGoals),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static TrainingAttendancePackage ToPackage(this TrainingAttendance source)
            => new()
            {
                PlayerId = source.Player.Id,
                Attendance = (int)source.Attendance,
                Reason = source.Reason,
                Rating = source.Rating,
                Comment = source.Comment,
                Id = source.Id
            };

        public static TrainingSessionPackage ToPackage(this TrainingSession source)
            => new()
            {
                StartDate = source.Start,
                EndDate = source.End,
                Theme = source.Theme,
                Place = source.Place,
                IsCancelled = source.IsCancelled,
                TeamIds = string.Join(";", source.TeamIds),
                Attendances = source.Attendances.Select(x => x.ToPackage()).ToList(),
                Stages = string.Join(";", source.Stages),
                TechnicalGoals = string.Join(";", source.TechnicalGoals),
                TacticalGoals = string.Join(";", source.TacticalGoals),
                PhysicalGoals = string.Join(";", source.PhysicalGoals),
                MentalGoals = string.Join(";", source.MentalGoals),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static SendedMailPackage ToPackage(this SendedMail source)
            => new()
            {
                Subject = source.Subject,
                Body = source.Body,
                SendACopy = source.SendACopy,
                Date = source.Date,
                State = (int)source.State,
                ToAddresses = string.Join(";", source.ToAddresses),
                CreatedAt = source.CreatedAt,
                CreatedBy = source.CreatedBy,
                ModifiedAt = source.ModifiedAt,
                ModifiedBy = source.ModifiedBy,
                Id = source.Id
            };

        public static StadiumPackage ToPackage(this Stadium source)
            => new()
            {
                Name = source.Name,
                Ground = (int)source.Ground,
                Address = source.Address?.ToPackage(),
                Id = source.Id
            };
    }
}
