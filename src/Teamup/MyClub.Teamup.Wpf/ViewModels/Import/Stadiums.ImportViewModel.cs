﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Selection;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class StadiumsImportDialogViewModel : SelectionDialogViewModel<StadiumImportableViewModel, StadiumsSelectionListViewModel>
    {
        private readonly ItemsSourceProvider<StadiumImportableViewModel> _provider;

        public StadiumsImportDialogViewModel(IImportStadiumsPlugin provider,
                                             StadiumService stadiumService,
                                             Func<StadiumImportableViewModel, bool> predicate)
            : this(new ItemsSourceProvider<StadiumImportableViewModel>(new PredicateItemsProvider<StadiumImportableViewModel>(
                                                                          provider.ProvideItems()
                                                                                  .Select(x => new StadiumImportableConverter(stadiumService)
                                                                                  .Convert(x)),
                                                                          predicate), false))
        { }

        private StadiumsImportDialogViewModel(ItemsSourceProvider<StadiumImportableViewModel> provider)
            : base(new StadiumsSelectionListViewModel(provider)) => _provider = provider;

        protected override void RefreshCore() => _provider.Reload();
    }

    internal class StadiumsSelectionListViewModel : SelectionListViewModel<StadiumImportableViewModel>
    {
        public StadiumsSelectionListViewModel(ISourceProvider<StadiumImportableViewModel> provider, SelectionMode selectionMode = SelectionMode.Single)
            : base(provider, new StadiumImportablesListParametersProvider(), selectionMode) { }
    }
}
