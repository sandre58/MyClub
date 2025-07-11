// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.Selection;
using MyNet.UI.Selection.Models;
using MyNet.UI.Threading;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal abstract class StagesViewModel<T> : SelectionListViewModel<T> where T : ICompetitionStageViewModel
    {
        protected StagesViewModel(ISourceProvider<T> matchStagesProvider, ListParametersProvider listParametersProvider, CompetitionCommandsService competitionCommandsService)
            : base(collection: new StagesCollection<T>(matchStagesProvider), parametersProvider: listParametersProvider)
        {
            Mode = ScreenMode.Read;

            OpenDateCommand = CommandsManager.Create<DateTime?>(x => NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeDay), x!.Value.ToDate()), x => x is not null);
            AddToDateCommand = CommandsManager.Create<DateTime>(async x => await AddItemAsync(x).ConfigureAwait(false));
            OpenBuildAssistantCommand = CommandsManager.Create(async () => await competitionCommandsService.OpenBuildAssistantAsync().ConfigureAwait(false));
        }

        public ICommand OpenDateCommand { get; }

        public ICommand AddToDateCommand { get; }

        public ICommand OpenBuildAssistantCommand { get; }

        protected override async void OpenCore(T item, int? selectedTab = null) => await item.OpenAsync().ConfigureAwait(false);

        protected override async Task<T?> CreateNewItemAsync()
        {
            await AddItemAsync().ConfigureAwait(false);

            return default;
        }

        protected override async Task<T?> UpdateItemAsync(T oldItem)
        {
            await oldItem.EditAsync().ConfigureAwait(false);

            return default;
        }

        public override async Task RemoveRangeAsync(IEnumerable<T> oldItems) => await RemoveItemsAsync(oldItems).ConfigureAwait(false);

        protected abstract Task AddItemAsync(DateTime? date = null);

        protected abstract Task RemoveItemsAsync(IEnumerable<T> oldItems);
    }

    internal class StagesCollection<T>(ISourceProvider<T> matchStagesSourceProvider)
        : SelectableCollection<T>(matchStagesSourceProvider.Connect(), scheduler: Scheduler.UI, createWrapper: x => new StageWrapper<T>(x)) where T : ICompetitionStageViewModel
    {
    }

    [CanSetIsModified(false)]
    [CanBeValidated(false)]
    internal class StageWrapper<T> : SelectedWrapper<T>, IAppointment where T : ICompetitionStageViewModel
    {
        public StageWrapper(T item) : base(item)
            => Disposables.AddRange(
            [
                item.WhenPropertyChanged(x => x.StartDate, false).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                }),
            ]);

        public DateTime StartDate => Item.StartDate;

        public DateTime EndDate => Item.EndDate;
    }
}
