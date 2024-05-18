// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class ImportStadiumsProvider : ItemsImportablesProvider<StadiumDto, StadiumImportableViewModel>
    {
        private readonly StadiumService _stadiumService;
        private readonly IImportStadiumsPlugin _importStadiumsPlugin;

        public ImportStadiumsProvider(IImportStadiumsPlugin importStadiumsPlugin, StadiumService stadiumService)
            : base(importStadiumsPlugin.ProvideItems)
        {
            _importStadiumsPlugin = importStadiumsPlugin;
            _stadiumService = stadiumService;
        }

        public override StadiumImportableViewModel Convert(StadiumDto dto)
            => new(dto.Name.OrEmpty(), _stadiumService.GetSimilarStadiums(dto.Name.OrEmpty(), dto.City).Any() ? ImportMode.Update : ImportMode.Add)
            {
                Address = dto.Street,
                City = dto.City,
                Ground = dto.Ground.OrEmpty().DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                PostalCode = dto.PostalCode,
                Country = dto.Country.OrEmpty().DehumanizeTo<Country>(OnNoMatch.ReturnsDefault)
            };

        public override bool CanImport() => _importStadiumsPlugin.CanImport();
    }
}
