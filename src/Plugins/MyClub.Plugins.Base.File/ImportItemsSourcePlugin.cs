// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Plugins.Base.File.ViewModels;
using MyClub.Plugins.Base.File.Views;
using MyNet.Utilities.IO;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Plugins.Base.File
{
    public abstract class ImportItemsSourcePlugin<T> : IDisposable
    {
        private bool _disposedValue;
        private readonly ItemsFileProvider<T> _fileProvider;
        private readonly FileSourceViewModel<T> _viewModel;

        public event EventHandler? ItemsLoadingRequested;

        protected ImportItemsSourcePlugin(ItemsFileProvider<T> fileProvider,
                                        FileExtensionInfo fileExtensionInfo,
                                        SampleFile? sampleFile = null)
        {
            _fileProvider = fileProvider;
            _viewModel = new FileSourceViewModel<T>(fileProvider, fileExtensionInfo, sampleFile);
            _viewModel.ItemsLoadingRequested += OnItemsLoadingRequested;
        }

        private void OnItemsLoadingRequested(object? sender, EventArgs e) => ItemsLoadingRequested?.Invoke(this, e);

        public object CreateView() => new FileSourceView() { DataContext = _viewModel };

        public Task InitializeAsync() => Task.CompletedTask;

        public IEnumerable<T> ProvideItems()
        {
            try
            {
                var items = _fileProvider.ProvideItems().ToList();
                _viewModel.SetExceptions(_fileProvider.Exceptions);

                return _fileProvider.Exceptions.Any() && !_viewModel.IgnoreErrors
                    ? throw new InvalidOperationException($"Items loaded from file '{_fileProvider.Filename}' with errors")
                    : (IEnumerable<T>)items;
            }
            finally
            {
                _viewModel.IgnoreErrors = false;
            }
        }

        public void Reload() => _viewModel.AskLoadingItems();

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _viewModel.ItemsLoadingRequested -= OnItemsLoadingRequested;
                    _viewModel.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
