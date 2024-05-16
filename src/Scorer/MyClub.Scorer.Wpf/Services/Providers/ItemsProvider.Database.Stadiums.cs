// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyClub.Scorer.Application.Services;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class StadiumsDatabaseProvider : ItemsDatabaseProvider<StadiumImportableViewModel>
    {
        private readonly StadiumService _stadiumService;

        public StadiumsDatabaseProvider(DatabaseService databaseService, StadiumService stadiumService, Func<StadiumImportableViewModel, bool>? predicate = null) : base(databaseService, predicate) => _stadiumService = stadiumService;

        public override IEnumerable<StadiumImportableViewModel> LoadItems()
            => DatabaseService.GetStadiums().Select(x => new StadiumImportableViewModel(x.Name.OrEmpty(), x.Address?.City, mode: _stadiumService.GetSimilarStadiums(x.Name.OrEmpty(), x.Address?.City).Any() ? ImportMode.Update : ImportMode.Add)
            {
                Address = x.Address?.Street,
                Ground = x.Ground,
                Latitude = x.Address?.Latitude,
                Longitude = x.Address?.Longitude,
                PostalCode = x.Address?.PostalCode,
                Country = x.Address?.Country
            });
    }
}
