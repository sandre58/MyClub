// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Scorer.Plugins.Contracts.Base;
using MyNet.Observable;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities.Converters;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal abstract class ImportSourceViewModel<TFrom, TTo> : ObservableObject, IImportSourceViewModel<TTo> where TTo : ImportableViewModel
    {
        private readonly IImportSourcePlugin<TFrom> _source;
        private readonly IConverter<TFrom, TTo> _converter;

        protected ImportSourceViewModel(IImportSourcePlugin<TFrom> source, IConverter<TFrom, TTo> converter)
        {
            _source = source;
            _converter = converter;
            View = source.CreateView();
            _source.ItemsLoadingRequested += OnItemsLoadingRequested;
        }

        public event EventHandler? ItemsLoadingRequested;

        public object View { get; }

        protected virtual TTo Convert(TFrom item) => _converter.Convert(item);

        private void OnItemsLoadingRequested(object? sender, EventArgs e) => ItemsLoadingRequested?.Invoke(this, e);

        public IEnumerable<TTo> ProvideItems() => _source.ProvideItems().Select(Convert);

        public void Reload() => _source.Reload();

        public async Task InitializeAsync() => await _source.InitializeAsync().ConfigureAwait(false);

        protected override void Cleanup()
        {
            _source.ItemsLoadingRequested -= OnItemsLoadingRequested;
            base.Cleanup();
        }
    }
}
