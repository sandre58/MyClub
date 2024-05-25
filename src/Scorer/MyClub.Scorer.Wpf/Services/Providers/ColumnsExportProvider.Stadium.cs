// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.Settings;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Converters;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    public class StadiumColumnsExportProvider : ColumnWrappersExportProviderBase<StadiumExportDto>
    {
        public static readonly IEnumerable<ColumnMapping<StadiumExportDto, object?>> DefaultColumns =
        [
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.Ground, nameof(MyClubResources.Ground), new EnumConverter<Ground>()),
            new(x => x.Street, nameof(MyClubResources.Street)),
            new(x => x.PostalCode, nameof(MyClubResources.PostalCode)),
            new(x => x.City, nameof(MyClubResources.City)),
            new(x => x.Country, nameof(MyClubResources.Country), new EnumerationConverter<Country>()),
            new(x => x.Longitude, nameof(MyClubResources.Longitude)),
            new(x => x.Latitude, nameof(MyClubResources.Latitude)),
        ];

        public StadiumColumnsExportProvider() : base(DefaultColumns, ExportStadiumsSettings.Default.ColumnsOrder, ExportStadiumsSettings.Default.SelectedColumns) { }
    }
}
