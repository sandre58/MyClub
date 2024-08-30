// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Application.Services;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Humanizer;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Resources;
using MyNet.UI.Services;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.Services
{
    internal abstract class PresentationServiceBase<TViewModel, TEditionViewModel, TService>
        where TEditionViewModel : IEntityEditionViewModel
        where TViewModel : IIdentifiable<Guid>
        where TService : ICrudService
    {
        protected TService Service { get; }

        protected IViewModelLocator ViewModelLocator { get; }

        protected PresentationServiceBase(TService service, IViewModelLocator viewModelLocator)
        {
            Service = service;
            ViewModelLocator = viewModelLocator;
        }

        public async Task<Guid?> AddAsync(Action<TEditionViewModel>? initialize = null)
        {
            var vm = ViewModelLocator.Get<TEditionViewModel>();
            vm.New(() => initialize?.Invoke(vm));

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.IsTrue() ? vm.ItemId : null;
        }

        public virtual async Task EditAsync(TViewModel item)
        {
            var vm = ViewModelLocator.Get<TEditionViewModel>();
            vm.Load(item.Id);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public virtual async Task RemoveAsync(IEnumerable<TViewModel> oldItems)
        {
            var idsList = oldItems.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateAndFormatWithCount(idsList.Count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() => Remove(idsList));
            }
        }

        protected virtual void Remove(ICollection<Guid> idsList) => Service.Remove(idsList);
    }
}
