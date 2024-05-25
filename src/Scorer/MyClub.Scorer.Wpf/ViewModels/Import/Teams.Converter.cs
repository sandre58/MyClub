// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Plugins.Contracts.Dtos;
using MyNet.Humanizer;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.Converters;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class TeamImportableConverter(TeamService teamService) : IConverter<TeamImportDto, TeamImportableViewModel>
    {
        private readonly TeamService _teamService = teamService;

        public TeamImportableViewModel Convert(TeamImportDto dto)
            => new(dto.Name.OrEmpty(), mode: _teamService.GetSimilarTeams(dto.Name.OrEmpty()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                ShortName = dto.ShortName.OrEmpty(),
                HomeColor = dto.HomeColor?.ToColor(),
                AwayColor = dto.AwayColor?.ToColor(),
                Logo = dto.Logo,
                Country = dto.Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                Stadium = dto.Stadium is not null
                       ? new StadiumImportableViewModel(dto.Stadium.Name.OrEmpty(), dto.Stadium.City)
                       {
                           Address = dto.Stadium.Street,
                           Country = dto.Stadium.Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                           Ground = dto.Stadium.Ground.OrEmpty().DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault),
                           Latitude = dto.Stadium.Latitude,
                           Longitude = dto.Stadium.Longitude,
                           PostalCode = dto.Stadium.PostalCode
                       } : null
            };

        public TeamImportDto ConvertBack(TeamImportableViewModel item) => throw new NotImplementedException();
    }
}
