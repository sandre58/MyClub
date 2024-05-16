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
                    StartDate = source.StartDate,
                    EndDate = source.EndDate,
                    Parameters = new ProjectParametersPackage
                    {
                        UseTeamVenues = source.Parameters.UseTeamVenues,
                        MatchStartTime = source.Parameters.MatchStartTime,
                        MinimumRestTime = source.Parameters.MinimumRestTime,
                        RotationTime = source.Parameters.RotationTime,
                    },
                    CreatedAt = source.CreatedAt,
                    CreatedBy = source.CreatedBy,
                    ModifiedAt = source.ModifiedAt,
                    ModifiedBy = source.ModifiedBy
                },
                Competition = source.Type switch
                {
                    CompetitionType.League => new LeaguePackage
                    {
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

        public static AddressPackage ToPackage(this Address address) => new()
        {
            City = address.City,
            Country = address.Country is not null ? (int)address.Country : null,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            PostalCode = address.PostalCode,
            Street = address.Street
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
                NeutralVenue = source.NeutralVenue,
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
    }
}
