﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MyClub.Plugins.Base.File.Resources;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Settings;
using MyNet.UI.Messages;
using MyNet.UI.Resources;
using MyNet.UI.Services;
using MyNet.UI.Toasting;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.Utilities.Localization;
using MyNet.Utilities.Messaging;

namespace MyClub.Plugins.Base.File.ViewModels
{
    public readonly struct SampleFile
    {
        public SampleFile(string fileName, string title, string description, FileExtensionInfo extensionInfo, Action<string> open)
        {
            FileName = fileName;
            Title = title;
            Description = description;
            ExtensionInfo = extensionInfo;
            Open = open;
        }

        public string FileName { get; }

        public string Title { get; }

        public string Description { get; }

        public FileExtensionInfo ExtensionInfo { get; }

        public Action<string> Open { get; }
    }

    public class FileSourceViewModel<T> : ObservableObject
    {
        private readonly ItemsFileProvider<T> _sourceProvider;
        private readonly FileExtensionInfo _fileExtensionInfo;
        private readonly SampleFile? _sampleFile;

        public event EventHandler? ItemsLoadingRequested;

        public FileSourceViewModel(ItemsFileProvider<T> sourceProvider,
                                   FileExtensionInfo fileExtensionInfo,
                                   SampleFile? sampleFile = null)
        {
            _sourceProvider = sourceProvider;
            _fileExtensionInfo = fileExtensionInfo;
            _sampleFile = sampleFile;

            OpenCurrentFileCommand = CommandsManager.Create(OpenCurrentFile, () => !string.IsNullOrEmpty(CurrentFilename));
            DownloadSampleFileCommand = CommandsManager.Create(async () => await DownloadSampleFileAsync().ConfigureAwait(false), () => _sampleFile is not null);
            ImportCommand = CommandsManager.Create(async () => await ImportAsync().ConfigureAwait(false));
            ReloadCommand = CommandsManager.Create(() => AskLoadingItems(false));
            IgnoreErrorsCommand = CommandsManager.Create(() => AskLoadingItems(true));

            TranslationService.RegisterResources(nameof(FileResources), FileResources.ResourceManager);
        }

        public string? CurrentFilename => _sourceProvider.Filename;

        public UiObservableCollection<Exception> Exceptions { get; } = [];

        public bool HasSampleFile => _sampleFile != null;

        public string? SampleFileDescription => _sampleFile?.Description;

        public bool IgnoreErrors { get; set; }

        public ICommand IgnoreErrorsCommand { get; set; }

        public ICommand ReloadCommand { get; set; }

        public ICommand DownloadSampleFileCommand { get; private set; }

        public ICommand OpenCurrentFileCommand { get; private set; }

        public ICommand ImportCommand { get; set; }

        private void OpenCurrentFile()
        {
            try
            {
                ProcessHelper.Open(CurrentFilename!);
            }
            catch (Exception e)
            {
                ToasterManager.ShowError(e.Message);
            }
        }

        public async Task ImportAsync()
        {
            var settings = new OpenFileDialogSettings()
            {
                Filters = _fileExtensionInfo.GetFileFilters()
            };
            var (result, filename) = await DialogManager.ShowOpenFileDialogAsync(settings).ConfigureAwait(false);

            if (result.IsFalse() || string.IsNullOrEmpty(filename)) return;

            if (!_fileExtensionInfo.IsValid(filename))
                throw new TranslatableException(MessageResources.FileHasInvalidExtensionError);

            _sourceProvider.SetFilename(filename);
            RaisePropertyChanged(nameof(CurrentFilename));
            AskLoadingItems(false);
        }

        public void AskLoadingItems(bool ignoreErrors = false)
        {
            IgnoreErrors = ignoreErrors;
            ItemsLoadingRequested?.Invoke(this, EventArgs.Empty);
        }

        public void SetExceptions(IEnumerable<Exception> exceptions) => Exceptions.Set(exceptions);

        private async Task DownloadSampleFileAsync()
        {
            if (_sampleFile is null) return;

            var settings = new SaveFileDialogSettings()
            {
                FileName = _sampleFile.Value.FileName,
                Filters = _sampleFile.Value.ExtensionInfo.GetFileFilters(),
                DefaultExtension = _sampleFile.Value.ExtensionInfo.GetDefaultExtension()
            };
            var result = await DialogManager.ShowSaveFileDialogAsync(settings).ConfigureAwait(false);

            if (result.result.IsTrue() && !string.IsNullOrEmpty(result.filename))
            {
                await AppBusyManager.BackgroundAsync(() =>
                {
                    try
                    {
                        using var stream = ResourcesHelper.ReadFromResourceFile(_sampleFile.Value.FileName, Assembly.GetExecutingAssembly());


                        if (stream is not null)
                        {
                            using var fileStream = new FileStream(result.filename, FileMode.Create, FileAccess.Write);
                            stream.CopyTo(fileStream);

                            Messenger.Default.Send(new FileExportedMessage(result.filename, _sampleFile.Value.Open));
                        }
                    }
                    catch (IOException)
                    {
                        throw new FileAlreadyUsedException(result.filename);
                    }
                });
            }
        }
    }
}
