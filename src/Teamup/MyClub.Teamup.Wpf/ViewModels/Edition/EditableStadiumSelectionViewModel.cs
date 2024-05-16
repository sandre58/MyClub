// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Threading;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableStadiumSelectionViewModel : ListViewModel<EditableStadiumViewModel>
    {
        private readonly StadiumPresentationService _stadiumPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly SingleTaskRunner _checkDatabaseConnectionRunner;

        public EditableStadiumSelectionViewModel(StadiumsProvider stadiumsProvider, StadiumPresentationService stadiumPresentationService)
            : base(parametersProvider: new ListParametersProvider([$"{nameof(EditableStadiumViewModel.Address)}.{nameof(Address.City)}", $"{nameof(EditableStadiumViewModel.Name)}"]))
        {
            _stadiumPresentationService = stadiumPresentationService;
            _stadiumsProvider = stadiumsProvider;
            _checkDatabaseConnectionRunner = new SingleTaskRunner(_ => CanImportFromDatabase = _stadiumPresentationService.CanImportFromDatabase());

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
            var item = await _stadiumPresentationService.ImportFromDatabaseAsync(Collection.Source.Select(x => (x.Name.OrEmpty(), x.Address?.City))).ConfigureAwait(false);

            if (item is not null)
                AddNewItem(item);
        }

        public override async Task AddAsync()
        {
            var item = await _stadiumPresentationService.CreateAsync(TextSearch).ConfigureAwait(false);

            if (item is not null)
                AddNewItem(item);
        }

        private void AddNewItem(EditableStadiumViewModel item)
        {
            Add(item);
            SelectedItem = item;
        }

        public override async Task EditAsync(EditableStadiumViewModel? oldItem)
        {
            if (oldItem is null) return;

            var result = await _stadiumPresentationService.UpdateAsync(oldItem).ConfigureAwait(false);

            if (result.IsTrue())
            {
                TextSearch = oldItem.DisplayName;
                SelectedItem = oldItem;
            }
        }

        public void Select(Guid? id) => SelectedItem = Items.FirstOrDefault(x => x.Id == id);

        public void Add(EditableStadiumViewModel item) => Collection.Contains(item).IfFalse(() => Collection.Add(item));

        protected override void ResetCore()
        {
            _checkDatabaseConnectionRunner.Run();
            SelectedItem = null;
            Collection.Set(_stadiumsProvider.Items.Select(x => new EditableStadiumViewModel(x.Id)
            {
                Name = x.Name,
                Ground = x.Ground,
                Address = x.Address
            }));
        }
    }
}
