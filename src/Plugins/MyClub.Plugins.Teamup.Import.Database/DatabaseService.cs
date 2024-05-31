// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyClub.DatabaseContext.Domain;
using MyClub.DatabaseContext.Domain.ClubAggregate;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;
using MyClub.DatabaseContext.Infrastructure.Data;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyNet.Utilities.Sequences;

namespace MyClub.Plugins.Teamup.Import.Database
{
    internal static class DatabaseService
    {
        public static MyClubContext CreateContext()
        {
            // Configuration
            var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName)
                                .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
                                .Build();

            var connectionString = configuration.GetConnectionString("Default");
            var optionsBuilder = new DbContextOptionsBuilder<MyClubContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new MyClubContext(optionsBuilder.Options);
        }

        public static IEnumerable<StadiumImportDto> LoadStadiums(IUnitOfWork unitOfWork)
            => unitOfWork.StadiumRepository.GetAll().Select(x => new StadiumImportDto
            {
                Name = x.Name,
                Ground = x.Ground,
                Street = x.Street,
                City = x.City,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                PostalCode = x.PostalCode,
                Country = x.Country
            });

        public static IEnumerable<TeamImportDto> LoadTeams(IUnitOfWork unitOfWork) => unitOfWork.TeamRepository.GetAll().Select(Convert);

        private static TeamImportDto Convert(Team x)
        {
            var clubStadium = x.Club.Stadium != null
                      ? new StadiumImportDto
                      {
                          Name = x.Club.Stadium.Name,
                          Ground = x.Club.Stadium.Ground,
                          Street = x.Club.Stadium.Street,
                          City = x.Club.Stadium.City,
                          Latitude = x.Club.Stadium.Latitude,
                          Longitude = x.Club.Stadium.Longitude,
                          PostalCode = x.Club.Stadium.PostalCode,
                          Country = x.Club.Stadium.Country
                      } : null;
            var teamStadium = x.Stadium != null
                      ? new StadiumImportDto
                      {
                          Name = x.Stadium.Name,
                          Ground = x.Stadium.Ground,
                          Street = x.Stadium.Street,
                          City = x.Stadium.City,
                          Latitude = x.Stadium.Latitude,
                          Longitude = x.Stadium.Longitude,
                          PostalCode = x.Stadium.PostalCode,
                          Country = x.Stadium.Country
                      } : null;
            return new TeamImportDto
            {
                Club = x.Club.Name,
                Logo = x.Club.Logo,
                Country = x.Club.Country,
                Name = x.Name,
                ShortName = x.ShortName,
                HomeColor = x.HomeColor ?? x.Club.HomeColor,
                AwayColor = x.AwayColor ?? x.Club.AwayColor,
                Category = x.Category,
                Stadium = teamStadium ?? clubStadium
            };
        }

        public static IEnumerable<CompetitionImportDto> LoadCompetitions(IUnitOfWork unitOfWork)
            => unitOfWork.CompetitionRepository.GetAll().Select(CreateCompetitionDto);

        private static CompetitionImportDto CreateCompetitionDto(Competition x)
        {
            var competition = new CompetitionImportDto
            {
                Type = x.Type,
                Name = x.Name,
                Category = x.Category,
                ShortName = x.ShortName,
                Logo = x.Logo,
                MatchTime = x.MatchTime.GetValueOrDefault(),
                RegulationTime = new HalfFormatImportDto
                {
                    Duration = x.RegulationTimeDuration,
                    Number = x.RegulationTimeNumber
                },
                ExtraTime = x.ExtraTimeNumber.HasValue && x.ExtraTimeDuration.HasValue
                            ? new HalfFormatImportDto
                            {
                                Duration = x.ExtraTimeDuration.Value,
                                Number = x.ExtraTimeNumber.Value
                            } : null,
                NumberOfShootouts = x.NumberOfPenaltyShootouts,
                ByGamesWon = x.PointsByGamesWon.GetValueOrDefault(3),
                ByGamesDrawn = x.PointsByGamesDrawn.GetValueOrDefault(1),
                ByGamesLost = x.PointsByGamesLost.GetValueOrDefault(0),
                RankingSortingColumns = x.SortingColumns?.Split(";").ToList() ?? [],
                HasExtraTime = x.ExtraTimeNumber > 0,
                HasShootouts = x.NumberOfPenaltyShootouts > 0,
                Labels = x.RankLabels?.Split(";").Select(x =>
                {
                    var label = x.Split(",");
                    var min = label.Length > 0 && int.TryParse(label[0], out var v1) ? v1 : 1;
                    var max = label.Length > 1 && int.TryParse(label[1], out var v2) ? v2 : 1;
                    var name = label.Length > 2 ? label[2] : string.Empty;
                    var shortName = label.Length > 3 ? label[3] : string.Empty;
                    var color = label.Length > 4 ? label[4] : string.Empty;
                    var description = label.Length > 5 ? label[5] : string.Empty;

                    return (min, max, new RankLabelImportDto
                    {
                        Color = color,
                        Description = description,
                        Name = name,
                        ShortName = shortName
                    });
                }).ToDictionary(x => ((int?)x.min, (int?)x.max), x => x.Item3),
            };

            return competition;
        }

        public static IEnumerable<PlayerImportDto> LoadPlayers(IUnitOfWork unitOfWork)
            => unitOfWork.PlayerRepository.GetAll().Select(x => new PlayerImportDto
            {
                LastName = x.LastName,
                FirstName = x.FirstName,
                Category = x.Category,
                Street = x.Street,
                City = x.City,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                PostalCode = x.PostalCode,
                AddressCountry = x.AddressCountry,
                Birthdate = x.Birthdate,
                Country = x.Country,
                Description = x.Description,
                Gender = x.Gender,
                Photo = x.Photo,
                PlaceOfBirth = x.PlaceOfBirth,
                Laterality = x.Laterality,
                LicenseNumber = x.LicenseNumber,
                Height = x.Height,
                Weight = x.Weight,
                Size = x.Size,
                FromDate = null,
                IsMutation = false,
                Number = null,
                LicenseState = null,
                ShoesSize = null,
                Emails = x.Emails != null ? x.Emails.Select(y => new ContactImportDto
                {
                    Default = y.Default,
                    Label = y.Label,
                    Value = y.Value,
                }).ToList() : null,
                Injuries = x.Injuries != null ? x.Injuries.Select(y => new InjuryImportDto()
                {
                    Category = y.Category,
                    Severity = y.Severity,
                    Type = y.Type,
                    Condition = y.Condition,
                    Date = y.StartDate,
                    Description = y.Description,
                    EndDate = y.EndDate,
                }).ToList() : null,
                Phones = x.Phones != null ? x.Phones.Select(y => new ContactImportDto
                {
                    Default = y.Default,
                    Label = y.Label,
                    Value = y.Value,
                }).ToList() : null,
                Positions = x.Positions != null ? x.Positions.Select(y => new RatedPositionImportDto()
                {
                    IsNatural = y.IsNatural,
                    Position = y.Position,
                    Rating = y.Rating
                }).ToList() : null,
            });
    }
}
