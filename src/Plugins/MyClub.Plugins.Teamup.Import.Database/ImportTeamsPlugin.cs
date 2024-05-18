// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.DatabaseContext.Domain;
using MyClub.Plugins.Teamup.Import.Database;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class ImportTeamsPlugin : ImportDatabaseItemsPlugin<TeamDto>, IImportTeamsPlugin
    {
        public override IEnumerable<TeamDto> LoadItems(IUnitOfWork unitOfWork)
            => unitOfWork.TeamRepository.GetAll().Select(x => new TeamDto
            {
                Club = new ClubDto
                {
                    Name = x.Club.Name,
                    Logo = x.Club.Logo,
                    Country = x.Club.Country,
                    HomeColor = x.Club.HomeColor,
                    AwayColor = x.Club.AwayColor,
                    Stadium = x.Club.Stadium != null
                      ? new StadiumDto
                      {
                          Name = x.Club.Stadium.Name,
                          Ground = x.Club.Stadium.Ground,
                          Street = x.Club.Stadium.Street,
                          City = x.Club.Stadium.City,
                          Latitude = x.Club.Stadium.Latitude,
                          Longitude = x.Club.Stadium.Longitude,
                          PostalCode = x.Club.Stadium.PostalCode,
                          Country = x.Club.Stadium.Country
                      } : null
                },
                Name = x.Name,
                ShortName = x.ShortName,
                HomeColor = x.HomeColor,
                AwayColor = x.AwayColor,
                Category = x.Category,
                Stadium = x.Stadium != null
                      ? new StadiumDto
                      {
                          Name = x.Stadium.Name,
                          Ground = x.Stadium.Ground,
                          Street = x.Stadium.Street,
                          City = x.Stadium.City,
                          Latitude = x.Stadium.Latitude,
                          Longitude = x.Stadium.Longitude,
                          PostalCode = x.Stadium.PostalCode,
                          Country = x.Stadium.Country
                      } : null
            });
    }
}
