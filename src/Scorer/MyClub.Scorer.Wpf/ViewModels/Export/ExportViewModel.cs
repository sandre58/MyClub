// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using GongSolutions.Wpf.DragDrop;
using MyClub.CrossCutting.Localization;
using MyNet.CsvHelper.Extensions;
using MyNet.Humanizer;
using MyNet.Observable.Attributes;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Settings;
using MyNet.UI.Resources;
using MyNet.UI.Selection.Models;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Scorer.Wpf.ViewModels.Export
{
    internal abstract class ExportViewModel<T, TDto> : WorkspaceDialogViewModel
    {
        private readonly string _defaultFolder;
        private readonly Func<string> _defaultExportName;
        private readonly FileExtensionInfo _fileExtensionInfo;
        private readonly IDictionary<ColumnMapping<TDto, object?>, bool> _defaultColumns;
        private IEnumerable<T>? _items;

        [IsFilePath(AllowEmpty = false)]
        [FolderExists]
        [Display(Name = nameof(Destination), ResourceType = typeof(MyClubResources))]
        public string? Destination { get; set; }

        public bool SaveConfiguration { get; set; } = true;

        public bool ShowHeaderColumnTraduction { get; set; } = true;

        public DefaultDropHandler DropHandler { get; } = new();

        public ICollection<DisplayWrapper<ICollection<string>>> PresetColumns { get; }

        public bool? AreAllSelected
        {
            get
            {
                var selected = Columns.Select(item => item.IsSelected).Distinct().ToList();
                return selected.Count == 1 ? selected.Single() : null;
            }
            set
            {
                if (value.HasValue)
                    Columns.ForEach(x => x.IsSelected = value.Value);
            }
        }

        public ICommand SetFilePathCommand { get; private set; }

        public ICommand ExportAndCloseCommand { get; private set; }

        public ICommand SetSelectedColumnsCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        [HasAnyItems]
        [Display(Name = nameof(Columns), ResourceType = typeof(MyClubResources))]
        public ObservableCollection<SelectedWrapper<ColumnMapping<TDto, object?>>> Columns { get; }

        protected ExportViewModel(FileExtensionInfo fileExtensionInfo,
                                  Func<string> defaultExportName,
                                  ColumnsExportProvider<TDto> columnsExportProvider,
                                  IEnumerable<DisplayWrapper<ICollection<string>>>? presetColumns = null,
                                  string? defaultFolder = null)
        {
            _fileExtensionInfo = fileExtensionInfo;
            _defaultColumns = columnsExportProvider.ProvideColumns();
            _defaultExportName = defaultExportName;
            _defaultFolder = !string.IsNullOrEmpty(defaultFolder) ? defaultFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            Columns = new(_defaultColumns.Select(x => new SelectedWrapper<ColumnMapping<TDto, object?>>(x.Key, x.Value)));

            PresetColumns = presetColumns?.ToList() ?? [];

            ExportAndCloseCommand = CommandsManager.Create(ExportAndClose);
            SetFilePathCommand = CommandsManager.Create(async () => await SetFilePathAsync().ConfigureAwait(false));
            SetSelectedColumnsCommand = CommandsManager.CreateNotNull<ICollection<string>>(SetSelectedColumns);
            CancelCommand = CommandsManager.Create(() => Close(false));

            Disposables.Add(Columns.ToObservableChangeSet(x => x.Id).WhenPropertyChanged(x => x.IsSelected).Subscribe(_ => RaisePropertyChanged(nameof(AreAllSelected))));

            ValidationRules.Add<ExportViewModel<T, TDto>, string?>(x => x.Destination, MessageResources.FileHasInvalidExtensionError, x => string.IsNullOrEmpty(x) || _fileExtensionInfo.IsValid(x));
        }

        public void Load(IEnumerable<T> items)
        {
            _items = items;
            var directory = Path.GetDirectoryName(Destination) ?? _defaultFolder;
            Destination = Path.Combine(directory, Path.ChangeExtension(_defaultExportName(), _fileExtensionInfo.GetDefaultExtension()));
            Title = MyClubResources.ExportXItems.FormatWith(_items.Count());
        }

        private async Task SetFilePathAsync()
        {
            var settings = new SaveFileDialogSettings()
            {
                FileName = Path.GetFileNameWithoutExtension(Destination) ?? string.Empty,
                InitialDirectory = !string.IsNullOrEmpty(Destination) ? Directory.Exists(Path.GetDirectoryName(Destination)) ? Path.GetDirectoryName(Destination) ?? string.Empty : string.Empty : string.Empty,
                Filters = _fileExtensionInfo.GetFileFilters(x => x.Translate()),
                DefaultExtension = _fileExtensionInfo.GetDefaultExtension()
            };
            var result = await DialogManager.ShowSaveFileDialogAsync(settings).ConfigureAwait(false);

            if (result.result.IsTrue() && !string.IsNullOrEmpty(result.filename))
                Destination = result.filename;
        }

        private void SetSelectedColumns(IEnumerable<string> columnNames) => Columns.ForEach(x => x.IsSelected = columnNames.Contains(x.Item.ResourceKey));

        private void ExportAndClose()
        {
            if (_items is null || !_items.Any())
                throw new TranslatableException(MyClubResources.ExportNoItemsError);

            if (!Columns.Any(x => x.IsSelected))
                throw new TranslatableException(MyClubResources.ExportNoColumnsError);

            if (ValidateProperties())
            {
                if (SaveConfiguration)
                    Save();

                Close(true);
            }
            else
            {
                var errors = GetErrors().ToList();
                errors.ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
            }
        }

        protected abstract void Save();

        private void SetColumnsOrder(IEnumerable<string> namesOrder)
        {
            var newIndex = 0;
            foreach (var item in namesOrder)
            {
                var currentIndex = Columns.Select(x => x.Item.ResourceKey).ToList().IndexOf(item);

                if (currentIndex > -1)
                {
                    Columns.Move(currentIndex, newIndex);
                    newIndex++;
                }
            }
        }

        protected override void ResetCore() => SetColumnsOrder(_defaultColumns.Select(x => x.Key.ResourceKey).ToList());
    }
}
