﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
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

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class StadiumImportableConverter(StadiumService stadiumService) : IConverter<StadiumImportDto, StadiumImportableViewModel>
    {
        private readonly StadiumService _stadiumService = stadiumService;

        public StadiumImportableViewModel Convert(StadiumImportDto dto)
            => new(dto.Name.OrEmpty(), mode: _stadiumService.GetSimilarStadiums(dto.Name.OrEmpty(), dto.City).Any() ? ImportMode.Update : ImportMode.Add)
            {
                Address = dto.Street,
                City = dto.City,
                Ground = dto.Ground.OrEmpty().DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                PostalCode = dto.PostalCode,
                Country = dto.Country.OrEmpty().DehumanizeTo<Country>(OnNoMatch.ReturnsDefault)
            };

        public StadiumImportDto ConvertBack(StadiumImportableViewModel item) => throw new NotImplementedException();
    }
}
