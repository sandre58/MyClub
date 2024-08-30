// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Threading;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class StadiumSelectionViewModel : ListViewModel<IStadiumViewModel>
    {
        private readonly StadiumPresentationService? _stadiumPresentationService;
        private readonly SingleTaskRunner _checkImportConnectionRunner;

        public StadiumSelectionViewModel(ISourceProvider<IStadiumViewModel> stadiumsProvider,
                                         StadiumPresentationService? stadiumPresentationService = null)
            : base(source: stadiumsProvider.Connect(), parametersProvider: new ListParametersProvider([$"{nameof(IStadiumViewModel.Address)}.{nameof(Address.City)}", $"{nameof(IStadiumViewModel.Name)}"]))
        {
            CanAdd = stadiumPresentationService is not null;
            CanEdit = stadiumPresentationService is not null;
            _stadiumPresentationService = stadiumPresentationService;
            _checkImportConnectionRunner = new SingleTaskRunner(_ => CanImport = stadiumPresentationService?.CanImport() ?? false);

            ImportCommand = CommandsManager.Create(async () => await ImportAsync().ConfigureAwait(false), () => SelectedItem is null && CanImport);

            Reset();
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public string? TextSearch { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanImport { get; private set; }

        public ICommand ImportCommand { get; }

        public async Task ImportAsync()
        {
            if (_stadiumPresentationService is null) return;

            var id = await _stadiumPresentationService.ImportAsync().ConfigureAwait(false);

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


        public override async Task EditAsync(IStadiumViewModel? oldItem)
        {
            if (oldItem is null || _stadiumPresentationService is null) return;

            await _stadiumPresentationService.EditAsync(oldItem).ConfigureAwait(false);
            RefreshText();
        }

        private void RefreshText() => TextSearch = SelectedItem?.DisplayName;

        protected override void ResetCore() => _checkImportConnectionRunner.Run();

        public void Select(Guid? id)
        {
            SelectedItem = id is null ? null : Collection.GetByIdOrDefault(id.Value);
            RefreshText();
        }
    }
}
