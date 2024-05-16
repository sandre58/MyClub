// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyNet.UI.Dialogs;
using MyNet.UI.Locators;
using MyNet.UI.Extensions;
using MyNet.Utilities;
using MyClub.Scorer.Wpf.ViewModels.Edition;

namespace MyClub.Scorer.Wpf.Services
{
    internal class LeaguePresentationService(IViewModelLocator viewModelLocator)
    {
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;

        public async Task EditRankingRulesAsync()
        {
            var vm = _viewModelLocator.Get<RankingRulesEditionViewModel>();

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task<EditableRankLabelViewModel?> CreateRankLabelAsync()
        {
            var vm = _viewModelLocator.Get<RankLabelEditionViewModel>();
            vm.New();

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                var item = new EditableRankLabelViewModel
                {
                    FromRank = vm.FromRank,
                    ToRank = vm.ToRank,
                    Color = vm.Color,
                    Description = vm.Description,
                    Name = vm.Name,
                    ShortName = vm.ShortName
                };

                return item;
            }

            return null;
        }

        public async Task<bool?> UpdateRankLabelAsync(EditableRankLabelViewModel oldItem)
        {
            var vm = _viewModelLocator.Get<RankLabelEditionViewModel>();
            vm.Load(oldItem);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                oldItem.Name = vm.Name;
                oldItem.ShortName = vm.ShortName;
                oldItem.FromRank = vm.FromRank;
                oldItem.ToRank = vm.ToRank;
                oldItem.Color = vm.Color;
                oldItem.Description = vm.Description;
            }

            return result;
        }
    }
}
