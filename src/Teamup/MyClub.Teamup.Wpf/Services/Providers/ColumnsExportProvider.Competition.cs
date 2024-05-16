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

namespace MyClub.Teamup.Wpf.ViewModels.Export
{
    public class CompetitionColumnsExportProvider : ColumnsExportProvider<CompetitionExportDto>
    {
        public static readonly IEnumerable<ColumnMapping<CompetitionExportDto, object?>> DefaultColumns =
        [
            new(x => x.Type, nameof(MyClubResources.Type)),
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.ShortName, nameof(MyClubResources.ShortName)),
            new(x => x.Category, nameof(MyClubResources.Category), new EnumerationConverter<Category>()),
            new(x => x.MatchTime, nameof(MyClubResources.DefaultTime)),
            new(x => x.HasExtraTime, nameof(MyClubResources.ExtraTime)),
            new(x => x.RegulationTime, nameof(MyClubResources.RegulationTimeFormat), new CsvHalfFormatConverter()),
            new(x => x.ExtraTime, nameof(MyClubResources.ExtraTimeFormat), new CsvHalfFormatConverter()),
            new(x => x.HasShootouts, nameof(MyClubResources.Shootouts)),
            new(x => x.NumberOfShootouts, nameof(MyClubResources.NumberOfShootouts)),
            new(x => x.ByGamesWon, nameof(MyClubResources.ByGamesWon)),
            new(x => x.ByGamesDrawn, nameof(MyClubResources.ByGamesDrawn)),
            new(x => x.ByGamesLost, nameof(MyClubResources.ByGamesLost)),
            new(x => x.RankingSortingColumns, nameof(MyClubResources.RankingSortingColumns), new EnumsConverter<RankingSortingColumn>()),
            new(x => x.Labels, nameof(MyClubResources.Labels), new CsvRankingLabelsConverter()),
        ];

        public CompetitionColumnsExportProvider() : base(DefaultColumns, ExportCompetitionsSettings.Default.ColumnsOrder, ExportCompetitionsSettings.Default.SelectedColumns) { }
    }
}
