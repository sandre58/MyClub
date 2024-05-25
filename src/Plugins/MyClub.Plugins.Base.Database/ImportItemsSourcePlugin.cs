// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MyClub.DatabaseContext.Infrastructure.Data;
using MyClub.Plugins.Base.Database.ViewModels;
using MyClub.Plugins.Base.Database.Views;

namespace MyClub.Plugins.Base.Database
{
    public abstract class ImportItemsSourcePlugin<T> : ImportItemsPlugin<T>
    {
        private readonly DatabaseSourceViewModel _viewModel;

        public event EventHandler? ItemsLoadingRequested;

        protected ImportItemsSourcePlugin(Func<MyClubContext> createContext)
            : base(createContext)
        {
            _viewModel = new DatabaseSourceViewModel(UnitOfWork);
            _viewModel.ItemsLoadingRequested += OnItemsLoadingRequested;
        }

        private void OnItemsLoadingRequested(object? sender, EventArgs e) => ItemsLoadingRequested?.Invoke(this, e);

        public object CreateView() => new DatabaseSourceView() { DataContext = _viewModel };

        public async Task InitializeAsync() => await _viewModel.InitializeAsync().ConfigureAwait(false);

        public void Reload() => _viewModel.AskLoadingItems();

        protected override void Dispose(bool disposing)
        {
            _viewModel.ItemsLoadingRequested -= OnItemsLoadingRequested;
            _viewModel.Dispose();
            base.Dispose(disposing);
        }
    }
}
