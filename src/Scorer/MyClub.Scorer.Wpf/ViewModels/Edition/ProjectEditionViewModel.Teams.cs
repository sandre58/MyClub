// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyClub.Scorer.Wpf.ViewModels.TeamsPage;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Dialogs;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectEditionTeamsViewModel : TeamsViewModelBase<EditableTeamViewModel>
    {
        private readonly PluginsService _pluginsService;
        private readonly ISourceProvider<EditableStadiumViewModel> _stadiumsProvider;

        public ProjectEditionTeamsViewModel(PluginsService pluginsService, ISourceProvider<EditableStadiumViewModel> stadiumsProvider)
            : base(pluginsService.HasPlugin<IImportTeamsSourcePlugin>())
        {
            _pluginsService = pluginsService;
            _stadiumsProvider = stadiumsProvider;
        }

        protected override Task AddItemAsync(string name)
        {
            var item = new EditableTeamViewModel
            {
                Name = name,
                ShortName = name.GetInitials(),
                HomeColor = RandomGenerator.Color().ToColor(),
                AwayColor = RandomGenerator.Color().ToColor()
            };
            Collection.Add(item);

            return Task.CompletedTask;
        }


        protected override async Task<EditableTeamViewModel?> CreateNewItemAsync()
        {
            using var vm = new EditableTeamEditionViewModel(_stadiumsProvider.Source);
            vm.New(MyClubResources.Team.Increment(Items.Select(y => y.Name).ToList(), format: " #"));

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.IsTrue()
                ? new EditableTeamViewModel
                {
                    Name = vm.Name,
                    ShortName = vm.ShortName,
                    AwayColor = vm.HomeColor,
                    HomeColor = vm.AwayColor,
                    Country = vm.Country,
                    Logo = vm.Logo,
                    Stadium = vm.Stadium
                }
                : null;
        }

        protected override async Task<EditableTeamViewModel?> UpdateItemAsync(EditableTeamViewModel oldItem)
        {
            using var vm = new EditableTeamEditionViewModel(_stadiumsProvider.Source);
            vm.Load(oldItem);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                oldItem.Name = vm.Name;
                oldItem.ShortName = vm.ShortName;
                oldItem.AwayColor = vm.AwayColor;
                oldItem.HomeColor = vm.HomeColor;
                oldItem.Logo = vm.Logo;
                oldItem.Country = vm.Country;
                oldItem.Stadium = vm.Stadium;
            }

            return null;
        }

        public override async Task RemoveRangeAsync(IEnumerable<EditableTeamViewModel> oldItems) => await ExecuteAsync(() => Collection.RemoveMany(oldItems)).ConfigureAwait(false);

        protected override Task ExportAsync() => throw new NotImplementedException();

        protected override async Task ImportAsync()

        {
            using var vm = new TeamsImportBySourcesDialogViewModel(new TeamsImportBySourcesProvider(_pluginsService, x => Items.Any(y => y.Name.Equals(x, StringComparison.OrdinalIgnoreCase))));

            if (vm.Sources.Count == 0) return;

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsFalse()) return;

            // Update teams
            vm.List.ImportItems.Where(x => x.Mode == ImportMode.Update).ForEach(x =>
            {
                var similarTeam = Items.FirstOrDefault(y => y.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));

                if (similarTeam != null)
                {
                    similarTeam.Name = x.Name;
                    similarTeam.ShortName = x.ShortName;
                    similarTeam.Logo = x.Logo;
                    similarTeam.AwayColor = x.AwayColor;
                    similarTeam.HomeColor = x.HomeColor;
                    similarTeam.Country = x.Country;
                    similarTeam.Stadium = x.Stadium is not null
                                            ? new EditableStadiumViewModel
                                            {
                                                Address = x.Stadium.GetAddress(),
                                                Ground = x.Stadium.Ground,
                                                Name = x.Name,
                                            } : null;
                }
            });

            // Add teams
            Collection.AddRange(vm.List.ImportItems.Where(x => x.Mode == ImportMode.Add).Select(x => new EditableTeamViewModel
            {
                Name = x.Name,
                ShortName = x.ShortName,
                AwayColor = x.AwayColor,
                Country = x.Country,
                HomeColor = x.HomeColor,
                Logo = x.Logo,
                Stadium = x.Stadium is not null
                              ? new EditableStadiumViewModel
                              {
                                  Address = x.Stadium.GetAddress(),
                                  Ground = x.Stadium.Ground,
                                  Name = x.Stadium.Name,
                              }
                              : null
            }));
        }

        protected override Task OnRemovingRequestedAsync(IEnumerable<EditableTeamViewModel> oldItems, CancelEventArgs cancelEventArgs) => Task.CompletedTask;

        protected override void ResetCore()
        {
            base.ResetCore();
            Collection.Clear();
        }

        public void Clear() => Collection.Clear();
    }
}
