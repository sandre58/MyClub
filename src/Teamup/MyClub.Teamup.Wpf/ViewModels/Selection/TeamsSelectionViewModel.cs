// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Selection;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.ViewModels.Import;

namespace MyClub.Teamup.Wpf.ViewModels.Selection
{
    internal class TeamsSelectionViewModel : ItemsSelectionViewModel<TeamImportableViewModel>
    {
        public TeamsSelectionViewModel(IItemsProvider<TeamImportableViewModel> itemsProvider, SelectionMode selectionMode = SelectionMode.Multiple)
            : base(itemsProvider,
                  parametersProvider: new TeamsSelectionListParametersProvider(),
                  selectionMode: selectionMode)
        { }
    }
}
