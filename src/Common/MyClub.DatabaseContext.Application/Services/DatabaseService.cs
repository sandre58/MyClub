// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.DatabaseContext.Application.Dtos;
using MyClub.DatabaseContext.Domain;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.DatabaseContext.Application.Services
{
    public class DatabaseService(IUnitOfWork unitOfWork)
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public IList<CompetitionDto> GetCompetitions() => _unitOfWork.CompetitionRepository.GetAll().AsEnumerable().Select(x =>
        {
            var competition = x.Type == Competition.League
                              ? new LeagueDto()
                              {
                                  PointsByGamesWon = x.PointsByGamesWon.GetValueOrDefault(3),
                                  PointsByGamesDrawn = x.PointsByGamesDrawn.GetValueOrDefault(1),
                                  PointsByGamesLost = x.PointsByGamesLost.GetValueOrDefault(0),
                                  SortingColumns = x.SortingColumns?.Split(";").Select(y => Enum.Parse<RankingSortingColumn>(y)).ToList() ?? []
                              }
                              : new CompetitionDto();
            competition.Id = x.Id;
            competition.Name = x.Name;
            competition.Category = x.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault);
            competition.ShortName = x.ShortName;
            competition.Logo = x.Logo;
            competition.MatchTime = x.MatchTime.GetValueOrDefault();
            competition.RegulationTimeDuration = x.RegulationTimeDuration;
            competition.RegulationTimeNumber = x.RegulationTimeNumber;
            competition.ExtraTimeDuration = x.ExtraTimeDuration;
            competition.ExtraTimeNumber = x.ExtraTimeNumber;
            competition.NumberOfPenaltyShootouts = x.NumberOfPenaltyShootouts;

            return competition;
        }).ToList();

        public IList<StadiumDto> GetStadiums() => [.. _unitOfWork.StadiumRepository.GetAll().Select(x => new StadiumDto
        {
            Id = x.Id,
            Name = x.Name,
            Ground = default,
            Address = new Address(x.Street, x.PostalCode, x.City, x.Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault), x.Latitude, x.Longitude)
        })];

        public IList<TeamDto> GetTeams() => [.. _unitOfWork.TeamRepository.GetAll().Select(x => new TeamDto
        {
            Id = x.Id,
            Club = new ClubDto
            {
                Name = x.Club.Name,
                Logo = x.Club.Logo,
                Country = x.Club.Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                HomeColor = x.Club.HomeColor,
                AwayColor = x.Club.AwayColor,
                Stadium = x.Club.Stadium != null
                      ? new StadiumDto
                      {
                          Id = x.Club.Stadium.Id,
                          Name = x.Club.Stadium.Name,
                          Ground = x.Club.Stadium.Ground.DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault),
                          Address = new Address(x.Club.Stadium.Street, x.Club.Stadium.PostalCode, x.Club.Stadium.City, !string.IsNullOrEmpty(x.Club.Stadium.Country) ? Country.FromName(x.Club.Stadium.Country, true) : null, x.Club.Stadium.Latitude, x.Club.Stadium.Longitude)
                      } : null
            },
            Name = x.Name,
            ShortName = x.ShortName,
            HomeColor = x.HomeColor,
            AwayColor = x.AwayColor,
            Category = x.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault),
            Stadium = x.Stadium != null
                      ? new StadiumDto
                      {
                          Id = x.Stadium.Id,
                          Name = x.Stadium.Name,
                          Ground = x.Stadium.Ground.DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault),
                          Address = new Address(x.Stadium.Street, x.Stadium.PostalCode, x.Stadium.City, !string.IsNullOrEmpty(x.Stadium.Country) ? Country.FromName(x.Stadium.Country, true) : null, x.Stadium.Latitude, x.Stadium.Longitude)
                      } : null
        })];

        public IList<PlayerDto> GetPlayers() => [.. _unitOfWork.PlayerRepository.GetAll().Select(x => new PlayerDto
        {
            Id = x.Id,
            LastName = x.LastName,
            FirstName = x.FirstName,
            Category = x.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault),
            Address = new Address(x.Street, x.PostalCode, x.City, x.Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault), x.Latitude, x.Longitude),
            Birthdate = x.Birthdate,
            Country = x.Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
            Description = x.Description,
            Gender = x.Gender.DehumanizeTo<GenderType>(OnNoMatch.ReturnsDefault),
            Photo = x.Photo,
            PlaceOfBirth = x.PlaceOfBirth,
            Laterality = x.Laterality.DehumanizeTo<Laterality>(OnNoMatch.ReturnsDefault),
            LicenseNumber = x.LicenseNumber,
            Height = x.Height,
            Weight = x.Weight,
            Size = x.Size,
            FromDate = null,
            Emails = x.Emails != null ? x.Emails.Select(y => new Email(y.Value.OrEmpty(), y.Label, y.Default)).ToList() : null,
            Injuries = x.Injuries != null ? x.Injuries.Select(y => new InjuryDto()
            {
                Category = y.Category.OrEmpty().DehumanizeTo<InjuryCategory>(OnNoMatch.ReturnsDefault),
                Severity = y.Severity.OrEmpty().DehumanizeTo<InjurySeverity>(OnNoMatch.ReturnsDefault),
                Type = y.Type.OrEmpty().DehumanizeTo<InjuryType>(OnNoMatch.ReturnsDefault),
                Condition = y.Condition,
                Date = y.StartDate,
                Description = y.Description,
                EndDate = y.EndDate,
                PlayerId = x.Id,
                Id = y.Id,
            }).ToList() : null,
            Phones = x.Phones != null ? x.Phones.Select(y => new Phone(y.Value.OrEmpty(), y.Label, y.Default)).ToList() : null,
            Positions = x.Positions != null ? x.Positions.Select(y => new RatedPositionDto()
            {
                Id = y.Id,
                IsNatural = y.IsNatural,
                Position = y.Position.OrEmpty().DehumanizeTo<Position>(OnNoMatch.ReturnsDefault),
                Rating = y.Rating.OrEmpty().DehumanizeTo<PositionRating>(OnNoMatch.ReturnsDefault)
            }).ToList() : null,
        })];

        public (string name, string host) GetConnectionInfo() => _unitOfWork.GetConnectionInfo();

        public bool CanConnect() => _unitOfWork.CanConnect();
    }
}
