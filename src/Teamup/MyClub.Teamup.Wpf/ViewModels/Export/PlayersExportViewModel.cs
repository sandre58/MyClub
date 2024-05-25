// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.Settings;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Humanizer;
using MyNet.Observable.Translatables;
using MyNet.UI.ViewModels.Export;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Teamup.Wpf.ViewModels.Export
{
    internal class PlayersExportViewModel(ProjectInfoProvider projectInfoProvider)
        : FileExportByColumnsViewModelBase<PlayerViewModel, ColumnMappingWrapper<SquadPlayerExportDto, object?>>(FileExtensionInfoProvider.Excel.Concat(FileExtensionInfoProvider.Csv),
               () => projectInfoProvider.ProvideExportName(MyClubResources.Players),
               new PlayerColumnsExportProvider().ProvideWrappers(),
               new List<DisplayWrapper<ICollection<string>>>
                    {
                        new(new [] { nameof(MyClubResources.Name), nameof(MyClubResources.FirstName), nameof(MyClubResources.Birthdate), nameof(MyClubResources.Age), nameof(MyClubResources.PlaceOfBirth), nameof(MyClubResources.Country), nameof(MyClubResources.Gender), nameof(MyClubResources.Street), nameof(MyClubResources.PostalCode), nameof(MyClubResources.City), nameof(MyClubResources.Email), nameof(MyClubResources.Phone) }, nameof(MyClubResources.General)),
                        new(new [] { nameof(MyClubResources.Name), nameof(MyClubResources.FirstName), nameof(MyClubResources.Birthdate), nameof(MyClubResources.Age), nameof(MyClubResources.Gender), nameof(MyClubResources.Team), nameof(MyClubResources.Category), nameof(MyClubResources.Number), nameof(MyClubResources.LicenseNumber), nameof(MyClubResources.LicenseState), nameof(MyClubResources.IsMutation), nameof(MyClubResources.InClubFromDate), nameof(MyClubResources.Position), nameof(MyClubResources.Positions)}, nameof(MyClubResources.Club)),
                        new(new [] { nameof(MyClubResources.Name), nameof(MyClubResources.FirstName), nameof(MyClubResources.Gender), nameof(MyClubResources.Team), nameof(MyClubResources.Category), nameof(MyClubResources.Laterality), nameof(MyClubResources.Height), nameof(MyClubResources.Weight), nameof(MyClubResources.Size), nameof(MyClubResources.ShoesSize)}, nameof(MyClubResources.Morphology))
                    },
               ExportPlayersSettings.Default.Folder)
    {
        protected override void SaveConfiguration()
        {
            ExportPlayersSettings.Default.ColumnsOrder = Columns.Select(x => x.Item.Key).Humanize(";");
            ExportPlayersSettings.Default.SelectedColumns = Columns.Where(x => x.IsSelected).Select(x => x.Item.Key).Humanize(";");
            ExportPlayersSettings.Default.Folder = Path.GetDirectoryName(Destination);

            ExportPlayersSettings.Default.Save();
        }

        protected override Task<bool> ExportItemsAsync(IEnumerable<PlayerViewModel> items) => Task.FromResult(true);
    }
}
