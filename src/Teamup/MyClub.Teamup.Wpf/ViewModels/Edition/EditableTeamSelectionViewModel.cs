// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Filters;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;
using MyNet.Utilities.Threading;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableTeamSelectionViewModel : ListViewModel<EditableTeamViewModel>
    {
        private readonly TeamPresentationService? _teamPresentationService;
        public event EventHandler<EditableTeamViewModel>? TeamCreated;
        private readonly SingleTaskRunner _checkDatabaseConnectionRunner;

        public EditableTeamSelectionViewModel(Subject<Func<EditableTeamViewModel, bool>>? filterChanged = null) : this(null, filterChanged) { }

        public EditableTeamSelectionViewModel(TeamPresentationService? teamPresentationService, Subject<Func<EditableTeamViewModel, bool>>? filterChanged = null)
            : base(parametersProvider: new ListParametersProvider($"{nameof(EditableTeamViewModel.Name)}"))
        {
            _teamPresentationService = teamPresentationService;
            _checkDatabaseConnectionRunner = new SingleTaskRunner(_ => CanImportFromDatabase = _teamPresentationService?.CanImportFromDatabase() ?? false);

            ImportFromDatabaseCommand = CommandsManager.Create(async () => await ImportFromDatabaseAsync().ConfigureAwait(false), () => SelectedItem is null && string.IsNullOrEmpty(TextSearch) && CanImportFromDatabase);

            if (filterChanged != null)
            {
                var filter = new ReferencesFilterViewModel<EditableTeamViewModel>(string.Empty, []) { Values = Collection.Source.AsEnumerable() };
                filterChanged.Subscribe(x =>
                {
                    filter.Values = Collection.Source.Where(x);
                    Collection.RefreshFilter();
                });
                Collection.Filters.Add(new CompositeFilter(filter));
            }

            Reset();
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public string? TextSearch { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanImportFromDatabase { get; private set; }

        public override bool CanAdd => _teamPresentationService is not null && SelectedItem is null && string.IsNullOrEmpty(TextSearch);

        public override bool CanEdit => _teamPresentationService is not null && SelectedItem is not null;

        public ICommand ImportFromDatabaseCommand { get; }

        public void Select(Guid? id) => SelectedItem = Items.FirstOrDefault(x => x.Id == id);

        public async Task ImportFromDatabaseAsync()
        {
            if (_teamPresentationService is null) return;

            var item = await _teamPresentationService.ImportFromDatabaseAsync(Collection.Source.Select(x => x.Name)).ConfigureAwait(false);

            if (item is not null)
                AddNewItem(item);
        }

        public override async Task AddAsync()
        {
            if (_teamPresentationService is null) return;

            var item = await _teamPresentationService.CreateAsync(Collection.Source.Select(x => x)).ConfigureAwait(false);

            if (item is not null)
                AddNewItem(item);
        }

        private void AddNewItem(EditableTeamViewModel item)
        {
            if (!Collection.Contains(item))
            {
                Collection.Add(item);
                SelectedItem = item;

                TeamCreated?.Invoke(this, item);
            }
        }

        public override async Task EditAsync(EditableTeamViewModel? oldItem)
        {
            if (oldItem is null || _teamPresentationService is null) return;

            var result = await _teamPresentationService.UpdateAsync(oldItem, Collection.Source.Except([oldItem])).ConfigureAwait(false);

            if (result.IsTrue())
            {
                TextSearch = oldItem.Name;
                SelectedItem = oldItem;
            }
        }

        protected override void ResetCore()
        {
            _checkDatabaseConnectionRunner.Run();
            SelectedItem = null;
            TextSearch = null;
        }

        public void UpdateSource(IEnumerable<EditableTeamViewModel> teams)
        {
            using (Collection.DeferFilter())
                Collection.Set(teams);
        }

        public void UpdateSource(IEnumerable<TeamViewModel> teams)
            => UpdateSource(teams.Select(x => new EditableTeamViewModel(x.Id)
            {
                ClubName = x.ClubName,
                Name = x.Name,
                ShortName = x.ShortName,
                AwayColor = x.AwayColor,
                HomeColor = x.HomeColor,
                Logo = x.Logo,
                Category = x.Category,
                Country = x.Country,
                IsMyTeam = x.IsMyTeam,
                Stadium = x.Stadium is StadiumViewModel stadium
                            ? new EditableStadiumViewModel(x.Stadium.Id)
                            {
                                Name = stadium.Name,
                                Address = stadium.Address,
                                Ground = stadium.Ground,
                            }
                            : null
            }).ToList());
    }
}
