// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Application.Services;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Import;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class StadiumsDatabaseProvider : ItemsDatabaseProvider<StadiumImportableViewModel>
    {
        private readonly StadiumService _stadiumService;

        public StadiumsDatabaseProvider(DatabaseService databaseService, StadiumService stadiumService, Func<StadiumImportableViewModel, bool>? predicate = null) : base(databaseService, predicate) => _stadiumService = stadiumService;

        public override IEnumerable<StadiumImportableViewModel> LoadItems()
            => DatabaseService.GetStadiums().Select(x => new StadiumImportableViewModel(x.Name.OrEmpty(), _stadiumService.GetSimilarStadiums(x.Name.OrEmpty(), x.Address?.City).Any() ? ImportMode.Update : ImportMode.Add)
            {
                Address = x.Address?.Street,
                City = x.Address?.City,
                Ground = x.Ground,
                Latitude = x.Address?.Latitude,
                Longitude = x.Address?.Longitude,
                PostalCode = x.Address?.PostalCode,
                Country = x.Address?.Country
            });
    }
}
