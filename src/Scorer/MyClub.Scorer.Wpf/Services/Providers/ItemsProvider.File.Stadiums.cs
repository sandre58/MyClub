// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.IO;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class StadiumsFileProvider : ItemsFileProvider<StadiumImportableViewModel>
    {
        private readonly StadiumsImportService _stadiumsImportService;
        private readonly StadiumService _stadiumService;

        public StadiumsFileProvider(StadiumsImportService stadiumsImportService, StadiumService stadiumService, Func<StadiumImportableViewModel, bool>? predicate = null) : base(predicate: predicate)
        {
            _stadiumService = stadiumService;
            _stadiumsImportService = stadiumsImportService;
        }

        protected override (IEnumerable<StadiumImportableViewModel> items, IEnumerable<Exception> exceptions) LoadItems(string filename)
        {
            ICollection<StadiumImportableViewModel> importableStadiums;

            var (stadiums, exceptions) = _stadiumsImportService.ExtractStadiums(filename);

            importableStadiums = stadiums.Select(x =>
            {
                var isSimilar = _stadiumService.GetSimilarStadiums(x.Name.OrEmpty(), x.City).Any();
                var stadium = new StadiumImportableViewModel(x.Name.OrEmpty(), x.City, mode: isSimilar ? ImportMode.Update : ImportMode.Add)
                {
                    Ground = x.Ground,
                    Address = x.Street,
                    PostalCode = x.PostalCode,
                    Country = x.Country,
                    Longitude = x.Longitude,
                    Latitude = x.Latitude,
                };

                stadium.ValidateProperties();
                return stadium;
            }).ToList();

            return new(importableStadiums, exceptions);
        }
    }
}
