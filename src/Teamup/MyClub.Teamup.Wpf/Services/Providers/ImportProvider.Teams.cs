// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class ImportTeamsProvider : ItemsImportablesProvider<TeamDto, TeamImportableViewModel>
    {
        private readonly TeamService _teamService;
        private readonly IImportTeamsPlugin _importTeamsPlugin;

        public ImportTeamsProvider(IImportTeamsPlugin importTeamsPlugin, TeamService teamService)
            : base(importTeamsPlugin.ProvideItems)
        {
            _importTeamsPlugin = importTeamsPlugin;
            _teamService = teamService;
        }

        public override TeamImportableViewModel Convert(TeamDto dto)
            => new(dto.Club.OrThrow().Name.OrEmpty(), _teamService.GetSimilarTeams(dto.Name.OrEmpty(), dto.Category.OrThrow().DehumanizeTo<Category>()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                AwayColor = dto.AwayColor?.ToColor() ?? dto.Club.OrThrow().AwayColor?.ToColor(),
                HomeColor = dto.HomeColor?.ToColor() ?? dto.Club.OrThrow().HomeColor?.ToColor(),
                Country = dto.Club.OrThrow().Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                Logo = dto.Club.OrThrow().Logo,
                ShortName = dto.ShortName.OrEmpty(),
                Name = dto.Name.OrEmpty(),
                Category = dto.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault),
                Stadium = (dto.Stadium ?? dto.Club.OrThrow().Stadium) is StadiumDto stadium
                          ? new StadiumImportableViewModel(stadium.Name.OrEmpty())
                          {
                              Ground = stadium.Ground.OrEmpty().DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault),
                              Address = stadium.Street,
                              City = stadium.City,
                              Country = stadium.Country.OrEmpty().DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                              Latitude = stadium.Latitude,
                              Longitude = stadium.Longitude,
                              PostalCode = stadium.PostalCode
                          } : null
            };

        public override bool CanImport() => _importTeamsPlugin.CanImport();
    }
}
