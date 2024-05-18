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
    internal class ImportStadiumsPlugin : ImportDatabaseItemsPlugin<StadiumDto>, IImportStadiumsPlugin
    {
        public override IEnumerable<StadiumDto> LoadItems(IUnitOfWork unitOfWork)
            => unitOfWork.StadiumRepository.GetAll().Select(x => new StadiumDto
            {
                Name = x.Name,
                Ground = x.Ground,
                Street = x.Street,
                City = x.City,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                PostalCode = x.PostalCode,
                Country = x.Country
            });
    }
}
