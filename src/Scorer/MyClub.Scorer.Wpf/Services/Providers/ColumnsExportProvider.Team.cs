// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Converters;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.Settings;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Converters;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    public class TeamColumnsExportProvider : ColumnsExportProvider<TeamDto>
    {
        public static readonly IEnumerable<ColumnMapping<TeamDto, object?>> DefaultColumns =
        [
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.ShortName, nameof(MyClubResources.ShortName)),
            new(x => x.Country, nameof(MyClubResources.Country), new EnumerationConverter<Country>()),
            new(x => x.Stadium, nameof(MyClubResources.Stadium), new CsvStadiumConverter()),
            new(x => x.HomeColor, nameof(MyClubResources.HomeColor)),
            new(x => x.AwayColor, nameof(MyClubResources.AwayColor)),
        ];

        public TeamColumnsExportProvider() : base(DefaultColumns, ExportTeamsSettings.Default.ColumnsOrder, ExportTeamsSettings.Default.SelectedColumns) { }
    }
}
