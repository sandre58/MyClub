// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Scorer.Plugins.Contracts.Dtos;
using MyNet.Humanizer;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.Converters;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumImportableConverter(Func<string, string?, bool> isSimilar) : IConverter<StadiumImportDto, StadiumImportableViewModel>
    {
        private readonly Func<string, string?, bool> _isSimilar = isSimilar;

        public StadiumImportableViewModel Convert(StadiumImportDto dto)
            => new(dto.Name.OrEmpty(), dto.City, mode: _isSimilar(dto.Name.OrEmpty(), dto.City) ? ImportMode.Update : ImportMode.Add)
            {
                Address = dto.Street,
                Ground = dto.Ground.OrEmpty().DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                PostalCode = dto.PostalCode,
                Country = dto.Country.OrEmpty().DehumanizeTo<Country>(OnNoMatch.ReturnsDefault)
            };

        public StadiumImportDto ConvertBack(StadiumImportableViewModel item) => throw new NotImplementedException();
    }
}
