// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Selection.Models;
using MyNet.UI.ViewModels.Import;
using MyNet.UI.ViewModels.List;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class ImportListViewModel<T> : ImportablesListViewModel<T> where T : notnull, ImportableViewModel
    {
        public ImportListViewModel(ISourceProvider<T> source, IListParametersProvider? parametersProvider = null)
            : base(source, parametersProvider) { }

        public IEnumerable? SelectedRows { get; set; }


        public void OnSelectedRowsChanged()
        {
            if (SelectedRows is null) return;

            UpdateSelection(SelectedRows.OfType<SelectedWrapper<T>>().Select(x => x.Item));
        }
    }
}
