// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.RankingAggregate;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Selection.Models;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Sequences;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    [CanBeValidatedForDeclaredClassOnly(true)]
    [CanSetIsModifiedAttributeForDeclaredClassOnly(true)]
    internal class RankingLabelsViewModel : ListViewModel<EditableRankLabelViewModel>
    {
        public RankingLabelsViewModel() : base()
        {
            AddOnRankCommand = CommandsManager.CreateNotNull<SelectedWrapper<EditableRankViewModel>>(async _ => await AddAsync().ConfigureAwait(false), x => IsFirstSelectedRank(x) && Ranks.SelectedItems.All(x => x.Label is null));
            RemoveOnRankCommand = CommandsManager.CreateNotNull<SelectedWrapper<EditableRankViewModel>>(async _ => await RemoveRangeAsync(Ranks.SelectedItems.Select(x => x.Label).NotNull().Distinct()).ConfigureAwait(false), x => IsFirstSelectedRank(x) && OneLabelIsFullSelected());
            EditOnRankCommand = CommandsManager.CreateNotNull<SelectedWrapper<EditableRankViewModel>>(async _ => await EditAsync(Ranks.SelectedItems.Select(x => x.Label).NotNull().FirstOrDefault()).ConfigureAwait(false), x => IsFirstSelectedRank(x) && OneLabelIsFullSelected());
            ReduceOnRankCommand = CommandsManager.CreateNotNull<SelectedWrapper<EditableRankViewModel>>(_ => UpdateRanksRange(Ranks.SelectedItems.Select(x => x.Label).NotNull().First()), x => IsFirstSelectedRank(x) && OneLabelIsPartiallySelected());
            ExpandOnRankCommand = CommandsManager.CreateNotNull<SelectedWrapper<EditableRankViewModel>>(_ => UpdateRanksRange(Ranks.SelectedItems.Select(x => x.Label).NotNull().First()), x => IsFirstSelectedRank(x) && OneLabelIsMoreSelected());

            Disposables.AddRange([
                Items.ToObservableChangeSet().Subscribe(_ => UpdateRanks()),
                Items.ToObservableChangeSet().WhenPropertyChanged(x => x.Range).Subscribe(_ => UpdateRanks()),
                ]);
        }

        public RanksListViewModel Ranks { get; } = new();

        public ICommand AddOnRankCommand { get; }

        public ICommand RemoveOnRankCommand { get; }

        public ICommand EditOnRankCommand { get; }

        public ICommand ReduceOnRankCommand { get; }

        public ICommand ExpandOnRankCommand { get; }

        public override bool IsModified() => Collection.Any(x => x.IsModified()) || base.IsModified();

        public void Load(Dictionary<AcceptableValueRange<int>, RankLabel> rankingLabels, int ranksCount)
        {
            Ranks.Reload(ranksCount);
            Collection.Set(rankingLabels.Select(x => new EditableRankLabelViewModel(new Interval<int>(x.Key.Min ?? 1, x.Key.Max ?? 1))
            {
                Color = x.Value.Color?.ToColor(),
                Description = x.Value.Description,
                Name = x.Value.Name,
                ShortName = x.Value.ShortName,
            }));
        }

        private bool IsFirstSelectedRank(SelectedWrapper<EditableRankViewModel> item) => item.IsSelected && item.Item.Item == Ranks.SelectedItems.Min(x => x.Item);

        private bool OneLabelIsFullSelected()
        {
            var selectedLabels = Ranks.SelectedItems.Select(x => x.Label).Distinct().ToList();

            return selectedLabels.Count == 1 && selectedLabels[0] is EditableRankLabelViewModel label && LabelIsFullSelected(label);
        }

        private bool OneLabelIsPartiallySelected()
        {
            var selectedLabels = Ranks.SelectedItems.Select(x => x.Label).Distinct().ToList();

            return selectedLabels.Count == 1 && selectedLabels[0] is EditableRankLabelViewModel label && !LabelIsFullSelected(label) && LabelIsPartiallySelected(label);
        }

        private bool OneLabelIsMoreSelected()
        {
            var selectedLabels = Ranks.SelectedItems.Select(x => x.Label).Distinct().ToList();

            return selectedLabels.Count == 2 && selectedLabels.NotNull().First() is EditableRankLabelViewModel label && LabelIsFullSelected(label) && Ranks.SelectedItems.Any(x => x.Label is null);
        }

        private bool LabelIsFullSelected(EditableRankLabelViewModel label)
            => EnumerableHelper.Range(label.Range.Start, label.Range.End, 1).All(x => Ranks.SelectedItems.Select(x => x.Item).Contains(x));

        private bool LabelIsPartiallySelected(EditableRankLabelViewModel label)
            => EnumerableHelper.Range(label.Range.Start, label.Range.End, 1).Any(x => Ranks.SelectedItems.Select(x => x.Item).Contains(x));

        protected override async Task<EditableRankLabelViewModel?> CreateNewItemAsync()
        {
            if (Ranks.SelectedWrappers.Count == 0) return null;

            var vm = new RankLabelEditionViewModel()
            {
                Name = MyClubResources.Rule.Increment(Items.Select(x => x.Name).ToList(), format: " #"),
                ShortName = MyClubResources.Rule.Substring(0, 3),
                Description = string.Empty,
                Color = RandomGenerator.Color().ToColor()
            };

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.IsTrue()
                ? new EditableRankLabelViewModel(new Interval<int>(Ranks.SelectedItems.MinOrDefault(x => x.Item), Ranks.SelectedItems.MaxOrDefault(x => x.Item)))
                {
                    Name = vm.Name,
                    Color = vm.Color,
                    Description = vm.Description,
                    ShortName = vm.ShortName,
                }
                : null;
        }

        protected override void EditItemCore(EditableRankLabelViewModel oldItem, EditableRankLabelViewModel newItem)
        {
            // No change collection
        }

        protected override async Task<EditableRankLabelViewModel?> UpdateItemAsync(EditableRankLabelViewModel oldItem)
        {
            var vm = new RankLabelEditionViewModel(oldItem);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                oldItem.Name = vm.Name;
                oldItem.Color = vm.Color;
                oldItem.Description = vm.Description;
                oldItem.ShortName = vm.ShortName;
            }

            return oldItem;
        }

        private void UpdateRanksRange(EditableRankLabelViewModel oldItem)
            => oldItem.Range = new Interval<int>(Ranks.SelectedItems.MinOrDefault(x => x.Item), Ranks.SelectedItems.MaxOrDefault(x => x.Item));

        protected override Task OnRemovingRequestedAsync(IEnumerable<EditableRankLabelViewModel> oldItems, CancelEventArgs cancelEventArgs) => Task.CompletedTask;

        private void UpdateRanks() => Ranks.Wrappers.ForEach(x => x.Item.Label = Collection.FirstOrDefault(y => y.Range.Contains(x.Item.Item)));
    }

    [CanBeValidatedForDeclaredClassOnly(true)]
    [CanSetIsModifiedAttributeForDeclaredClassOnly(true)]
    internal class RanksListViewModel : SelectionListViewModel<EditableRankViewModel>
    {
        public void Reload(int count) => Collection.Set(EnumerableHelper.Range(1, count, 1).Select(x => new EditableRankViewModel(x)).ToList());

        public override bool IsModified() => Collection.Any(x => x.IsModified());
    }

    internal class EditableRankViewModel : EditableWrapper<int>
    {
        public EditableRankViewModel(int item) : base(item)
        {
        }

        public EditableRankLabelViewModel? Label { get; set; }

        public override bool Equals(object? obj) => obj is EditableRankViewModel rank && rank.Item == Item;

        public override int GetHashCode() => Item.GetHashCode();
    }
}
