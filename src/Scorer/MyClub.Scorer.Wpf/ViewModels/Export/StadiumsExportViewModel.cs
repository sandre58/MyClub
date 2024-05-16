// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using MyNet.Humanizer;
using MyNet.Utilities.IO.FileExtensions;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.Settings;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Application.Dtos;

namespace MyClub.Scorer.Wpf.ViewModels.Export
{
    internal class StadiumsExportViewModel(ProjectInfoProvider projectInfoProvider) : ExportViewModel<StadiumViewModel, StadiumExportDto>(FileExtensionInfoProvider.Excel.Concat(FileExtensionInfoProvider.Csv),
               () => projectInfoProvider.ProvideExportName(MyClubResources.Stadiums),
               new StadiumColumnsExportProvider(),
               defaultFolder: ExportStadiumsSettings.Default.Folder)
    {
        protected override void Save()
        {
            ExportStadiumsSettings.Default.ColumnsOrder = Columns.Select(x => x.Item.ResourceKey).Humanize(";");
            ExportStadiumsSettings.Default.SelectedColumns = Columns.Where(x => x.IsSelected).Select(x => x.Item.ResourceKey).Humanize(";");
            ExportStadiumsSettings.Default.Folder = Path.GetDirectoryName(Destination);

            ExportStadiumsSettings.Default.Save();
        }
    }
}
