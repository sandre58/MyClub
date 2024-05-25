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
using MyClub.DatabaseContext.Infrastructure.Data;
using MyClub.Scorer.Plugins.Contracts.Dtos;

namespace MyClub.Plugins.Scorer.Import.Database
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
                Name = x.Club.Name,
                ShortName = x.ShortName,
                Logo = x.Club.Logo,
                Country = x.Club.Country,
                HomeColor = x.HomeColor ?? x.Club.HomeColor,
                AwayColor = x.AwayColor ?? x.Club.AwayColor,
                Stadium = teamStadium ?? clubStadium
            };
        }
    }
}
