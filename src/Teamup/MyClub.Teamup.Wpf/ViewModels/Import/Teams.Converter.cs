// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyNet.Humanizer;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.Converters;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class TeamImportableConverter(TeamService teamService) : IConverter<TeamImportDto, TeamImportableViewModel>
    {
        private readonly TeamService _teamService = teamService;

        public TeamImportableViewModel Convert(TeamImportDto dto)
            => new(dto.Club.OrEmpty(), _teamService.GetSimilarTeams(dto.Name.OrEmpty(), dto.Category.OrThrow().DehumanizeTo<Category>()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                AwayColor = dto.AwayColor?.ToColor(),
                HomeColor = dto.HomeColor?.ToColor(),
                Country = dto.Country.OrEmpty().DehumanizeTo<Country>(OnNoMatch.ReturnsDefault),
                Logo = dto.Logo,
                ShortName = dto.ShortName.OrEmpty(),
                Name = dto.Name.OrEmpty(),
                Category = dto.Category.OrEmpty().DehumanizeTo<Category>(OnNoMatch.ReturnsDefault),
                Stadium = dto.Stadium is StadiumImportDto stadium
                          ? new StadiumImportableViewModel(stadium.Name.OrEmpty())
                          {
                              Ground = stadium.Ground.OrEmpty().DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault),
                              Address = stadium.Street,
                              City = stadium.City,
                              Country = stadium.Country.OrEmpty().DehumanizeTo<Country>(OnNoMatch.ReturnsDefault),
                              Latitude = stadium.Latitude,
                              Longitude = stadium.Longitude,
                              PostalCode = stadium.PostalCode
                          } : null
            };

        public TeamImportDto ConvertBack(TeamImportableViewModel item) => throw new NotImplementedException();
    }
}
