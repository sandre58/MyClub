// Copyright (c) Stéphane ANDRE. All Right Reserved.
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
    internal class TeamsImportDialogViewModel : SelectionDialogViewModel<TeamImportableViewModel, TeamsSelectionListViewModel>
    {
        private readonly ItemsSourceProvider<TeamImportableViewModel> _provider;

        public TeamsImportDialogViewModel(IImportTeamsPlugin provider,
                                             TeamService teamService,
                                             Func<TeamImportableViewModel, bool> predicate)
            : this(new ItemsSourceProvider<TeamImportableViewModel>(new PredicateItemsProvider<TeamImportableViewModel>(
                                                                          provider.ProvideItems()
                                                                                  .Select(x => new TeamImportableConverter(teamService)
                                                                                  .Convert(x)),
                                                                          predicate), false))
        { }

        private TeamsImportDialogViewModel(ItemsSourceProvider<TeamImportableViewModel> provider)
            : base(new TeamsSelectionListViewModel(provider)) => _provider = provider;

        protected override void RefreshCore() => _provider.Reload();
    }

    internal class TeamsSelectionListViewModel : SelectionListViewModel<TeamImportableViewModel>
    {
        public TeamsSelectionListViewModel(ISourceProvider<TeamImportableViewModel> provider, SelectionMode selectionMode = SelectionMode.Single)
            : base(provider, new TeamImportablesListParametersProvider(), selectionMode) { }
    }
}
