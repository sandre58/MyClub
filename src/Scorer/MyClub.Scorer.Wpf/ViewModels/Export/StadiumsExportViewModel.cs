// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.Settings;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Humanizer;
using MyNet.UI.ViewModels.Export;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Scorer.Wpf.ViewModels.Export
{
    internal class StadiumsExportViewModel(ProjectInfoProvider projectInfoProvider)
        : FileExportByColumnsViewModelBase<IStadiumViewModel, ColumnMappingWrapper<StadiumExportDto, object?>>(FileExtensionInfoProvider.Excel.Concat(FileExtensionInfoProvider.Csv),
               () => projectInfoProvider.ProvideExportName(MyClubResources.Stadiums),
               new StadiumColumnsExportProvider().ProvideWrappers(),
               defaultFolder: ExportStadiumsSettings.Default.Folder)
    {
        protected override void SaveConfiguration()
        {
            ExportStadiumsSettings.Default.ColumnsOrder = Columns.Select(x => x.Item.Key).Humanize(";");
            ExportStadiumsSettings.Default.SelectedColumns = Columns.Where(x => x.IsSelected).Select(x => x.Item.Key).Humanize(";");
            ExportStadiumsSettings.Default.Folder = Path.GetDirectoryName(Destination);

            ExportStadiumsSettings.Default.Save();
        }

        protected override Task<bool> ExportItemsAsync(IEnumerable<IStadiumViewModel> items) => Task.FromResult(true);
    }
}
