// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using MyNet.Humanizer;
using MyNet.Utilities.IO.FileExtensions;
using MyClub.Teamup.Application.Dtos;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.Settings;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.Export
{
    internal class CompetitionsExportViewModel(ProjectInfoProvider projectInfoProvider) : ExportViewModel<CompetitionViewModel, CompetitionExportDto>(FileExtensionInfoProvider.Excel.Concat(FileExtensionInfoProvider.Csv),
               () => projectInfoProvider.ProvideExportName(MyClubResources.Competitions),
               new CompetitionColumnsExportProvider(),
               defaultFolder: ExportCompetitionsSettings.Default.Folder)
    {
        protected override void Save()
        {
            ExportCompetitionsSettings.Default.ColumnsOrder = Columns.Select(x => x.Item.ResourceKey).Humanize(";");
            ExportCompetitionsSettings.Default.SelectedColumns = Columns.Where(x => x.IsSelected).Select(x => x.Item.ResourceKey).Humanize(";");
            ExportCompetitionsSettings.Default.Folder = Path.GetDirectoryName(Destination);

            ExportCompetitionsSettings.Default.Save();
        }
    }
}
