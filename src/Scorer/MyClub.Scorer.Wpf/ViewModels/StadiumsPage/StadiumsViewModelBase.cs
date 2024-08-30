// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.StadiumsPage
{
    internal abstract class StadiumsViewModelBase<T> : SelectionListViewModel<T>
        where T : IStadiumViewModel
    {
        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public string? SpeedItemName { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool HasImportSources { get; private set; }

        public ICommand AddSpeedItemCommand { get; private set; }

        public ICommand SpeedAddCommand { get; private set; }

        public ICommand ExportCommand { get; private set; }

        public ICommand ImportCommand { get; private set; }

        protected StadiumsViewModelBase(bool canImport, IListParametersProvider? parametersProvider = null)
            : base(parametersProvider: parametersProvider)
        {
            HasImportSources = canImport;

            SpeedAddCommand = CommandsManager.Create<double>(async x => await SpeedAddAsync((int)x).ConfigureAwait(false), _ => CanAdd);
            AddSpeedItemCommand = CommandsManager.Create(async () => await AddSpeedItemAsync().ConfigureAwait(false), () => CanAdd && !string.IsNullOrEmpty(SpeedItemName));
            ExportCommand = CommandsManager.Create(async () => await ExportAsync().ConfigureAwait(false), () => Items.Any());
            ImportCommand = CommandsManager.Create(async () => await ImportAsync().ConfigureAwait(false), () => HasImportSources);
        }

        protected StadiumsViewModelBase(ISourceProvider<T> stadiumsProvider, bool canImport, IListParametersProvider? parametersProvider = null)
            : base(source: stadiumsProvider.Connect(),
                   parametersProvider: parametersProvider)
        {
            HasImportSources = canImport;

            SpeedAddCommand = CommandsManager.Create<int>(async x => await SpeedAddAsync(x).ConfigureAwait(false), _ => CanAdd);
            AddSpeedItemCommand = CommandsManager.Create(async () => await AddSpeedItemAsync().ConfigureAwait(false), () => CanAdd && !string.IsNullOrEmpty(SpeedItemName));
            ExportCommand = CommandsManager.Create(async () => await ExportAsync().ConfigureAwait(false), () => Items.Any());
            ImportCommand = CommandsManager.Create(async () => await ImportAsync().ConfigureAwait(false), () => HasImportSources);
        }

        protected abstract Task AddItemAsync(string name);

        protected abstract Task ExportAsync();

        protected abstract Task ImportAsync();

        public abstract override Task RemoveRangeAsync(IEnumerable<T> oldItems);

        protected override void OnAddCompleted(T item)
        {
            if (Items.Contains(item))
                Collection.SetSelection([item]);
        }

        private async Task SpeedAddAsync(int count)
        {
            var currentNames = Items.Select(y => y.Name).ToList();
            var newNames = new List<string>();
            Math.Max(1, count).Iteration(_ => newNames.Add(MyClubResources.Stadium.Increment(currentNames.Union(newNames).ToList(), format: " #")));

            await ExecuteAsync(() => newNames.ForEach(async x => await AddItemAsync(x).ConfigureAwait(false))).ConfigureAwait(false);
        }

        private async Task AddSpeedItemAsync()
        {
            if (!string.IsNullOrEmpty(SpeedItemName))
            {
                await AddItemAsync(SpeedItemName).ConfigureAwait(false);

                SpeedItemName = null;
            }
        }

        protected override void ResetCore()
        {
            base.ResetCore();
            SpeedItemName = null;
        }
    }
}
