// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Converters;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Wpf.Settings;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Converters;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Wpf.ViewModels.Export
{
    public class TeamColumnsExportProvider : ColumnsExportProvider<TeamExportDto>
    {
        public static readonly IEnumerable<ColumnMapping<TeamExportDto, object?>> DefaultColumns =
        [
            new(x => x.Club, nameof(MyClubResources.Club)),
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.ShortName, nameof(MyClubResources.ShortName)),
            new(x => x.Category, nameof(MyClubResources.Category), new EnumerationConverter<Category>()),
            new(x => x.Country, nameof(MyClubResources.Country), new EnumerationConverter<Country>()),
            new(x => x.Stadium, nameof(MyClubResources.Stadium), new CsvStadiumConverter()),
            new(x => x.HomeColor, nameof(MyClubResources.HomeColor)),
            new(x => x.AwayColor, nameof(MyClubResources.AwayColor)),
        ];

        public TeamColumnsExportProvider() : base(DefaultColumns, ExportTeamsSettings.Default.ColumnsOrder, ExportTeamsSettings.Default.SelectedColumns) { }
    }
}
