// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Threading;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class StadiumSelectionViewModel : ListViewModel<StadiumViewModel>
    {
        private readonly StadiumPresentationService? _stadiumPresentationService;
        private readonly SingleTaskRunner _checkDatabaseConnectionRunner;

        public StadiumSelectionViewModel(ISourceProvider<StadiumViewModel> stadiumsProvider,
                                         StadiumPresentationService? stadiumPresentationService = null,
                                         DatabaseService? databaseService = null)
            : base(source: stadiumsProvider.Connect(), parametersProvider: new ListParametersProvider([$"{nameof(StadiumViewModel.Address)}.{nameof(Address.City)}", $"{nameof(StadiumViewModel.Name)}"]))
        {
            CanAdd = stadiumPresentationService is not null;
            CanEdit = stadiumPresentationService is not null;
            _stadiumPresentationService = stadiumPresentationService;
            _checkDatabaseConnectionRunner = new SingleTaskRunner(_ => CanImportFromDatabase = databaseService?.CanConnect() ?? false);

            ImportFromDatabaseCommand = CommandsManager.Create(async () => await ImportFromDatabaseAsync().ConfigureAwait(false), () => SelectedItem is null && CanImportFromDatabase);

            Reset();
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public string? TextSearch { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanImportFromDatabase { get; private set; }

        public ICommand ImportFromDatabaseCommand { get; }

        public async Task ImportFromDatabaseAsync()
        {
            if (_stadiumPresentationService is null) return;

            var id = await _stadiumPresentationService.ImportFromDatabaseAsync().ConfigureAwait(false);

            if (id.HasValue)
                Select(id);
        }

        public override async Task AddAsync()
        {
            if (_stadiumPresentationService is null) return;

            var id = await _stadiumPresentationService.AddAsync(x => TextSearch.IfNotNull(y => x.Name = y)).ConfigureAwait(false);

            if (id.HasValue)
                Select(id);
        }


        public override async Task EditAsync(StadiumViewModel? oldItem)
        {
            if (oldItem is null || _stadiumPresentationService is null) return;

            await _stadiumPresentationService.EditAsync(oldItem).ConfigureAwait(false);
            RefreshText();
        }

        private void RefreshText() => TextSearch = SelectedItem?.DisplayName;

        protected override void ResetCore() => _checkDatabaseConnectionRunner.Run();

        public void Select(Guid? id)
        {
            SelectedItem = id is null ? null : Collection.GetByIdOrDefault(id.Value);
            RefreshText();
        }
    }
}
