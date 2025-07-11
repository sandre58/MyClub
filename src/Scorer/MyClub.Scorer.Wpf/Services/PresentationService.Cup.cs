// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyClub.Scorer.Wpf.ViewModels.BuildAssistant;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;

namespace MyClub.Scorer.Wpf.Services
{
    internal class CupPresentationService(IViewModelLocator viewModelLocator)
    {
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;

        public async Task OpenBuildAssistantAsync()
        {
            var vm = _viewModelLocator.Get<CupBuildAssistantViewModel>();

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }
    }
}
