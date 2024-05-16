// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GongSolutions.Wpf.DragDrop;
using MyNet.UI.Commands;
using MyNet.UI.Selection.Models;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyClub.Teamup.Domain.Enums;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TacticPage
{
    internal class TacticsViewModel : SelectionListViewModel<TacticViewModel>
    {
        internal class TacticDropHandler : DefaultDropHandler
        {
            private readonly TacticPresentationService _tacticPresentationService;

            public TacticDropHandler(TacticPresentationService tacticPresentationService) => _tacticPresentationService = tacticPresentationService;

            public override void Drop(IDropInfo dropInfo)
            {
                var data = dropInfo.Data as SelectedWrapper<TacticViewModel>;

                if (data is not null)
                {
                    var list = dropInfo.TargetCollection.OfType<SelectedWrapper<TacticViewModel>>().Except(new[] { data }).ToList();
                    list.Insert(Math.Max(0, dropInfo.InsertIndex - 1), data);
                    list.Select((item, index) => (item, index)).ForEach(x => _tacticPresentationService.SetOrder(x.item.Item, x.index));
                }
            }
        }

        private readonly TacticPresentationService _tacticPresentationService;

        public override bool CanOpen => true;

        public override bool CanAdd => true;

        public TacticDropHandler DropHandler { get; }

        public DefaultDragHandler DragHandler { get; } = new();

        public TacticDetailsViewModel TacticDetailsViewModel { get; }

        public ICommand AddKnownTacticCommand { get; }

        public ICommand DuplicateSelectedItemCommand { get; }

        public TacticsViewModel(
            ProjectInfoProvider projectInfoProvider,
            TacticsProvider tacticsProvider,
            PlayersProvider playersProvider,
            TacticPresentationService tacticPresentationService)
            : base(source: tacticsProvider.Connect(),
                  parametersProvider: new TacticsListParametersProvider())
        {
            _tacticPresentationService = tacticPresentationService;
            DropHandler = new(_tacticPresentationService);

            TacticDetailsViewModel = new TacticDetailsViewModel(projectInfoProvider, playersProvider);

            AddKnownTacticCommand = CommandsManager.CreateNotNull<KnownTactic>(async x => await _tacticPresentationService.AddAsync(x).ConfigureAwait(false));
            DuplicateSelectedItemCommand = CommandsManager.Create(async () => await _tacticPresentationService.DuplicateAsync(SelectedItem!).ConfigureAwait(false), () => SelectedWrappers.Count == 1);
        }

        protected override string CreateTitle() => MyClubResources.Tactics;

        protected override void OpenCore(TacticViewModel? item, int? selectedTab = null) => item?.Open();

        protected override async Task<TacticViewModel?> CreateNewItemAsync()
        {
            var id = await _tacticPresentationService.AddAsync().ConfigureAwait(false);

            return Source.GetByIdOrDefault(id.GetValueOrDefault());
        }

        protected override void OnAddCompleted(TacticViewModel item)
        {
            if (Items.Contains(item))
                Collection.SetSelection([item]);
        }

        protected override async Task<TacticViewModel?> UpdateItemAsync(TacticViewModel oldItem)
        {
            await _tacticPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        public override async Task RemoveRangeAsync(IEnumerable<TacticViewModel> oldItems) => await _tacticPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            if (SelectedWrappers.Count != 1)
            {
                TacticDetailsViewModel.SetItem(null);
                return;
            }

            if (SelectedItem?.Id != TacticDetailsViewModel.Item?.Id)
                TacticDetailsViewModel.SetItem(SelectedItem);
        }

        public void ShowDetails(TacticViewModel item)
        {
            UpdateSelection([item]);
            Display.SetMode<DisplayModeDetailled>();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            TacticDetailsViewModel.Dispose();
        }
    }
}
