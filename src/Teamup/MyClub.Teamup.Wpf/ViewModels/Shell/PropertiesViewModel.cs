// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.UI.Helpers;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;

namespace MyClub.Teamup.Wpf.ViewModels.Shell
{
    internal class PropertiesViewModel : WorkspaceViewModel
    {
        private readonly ProjectInfoProvider _projectInfoProvider;

        public Project? CurrentProject { get; private set; }

        public bool HasCurrentProject { get; private set; }

        public DateTime? CreatedDate => _projectInfoProvider.CreatedDate;

        public DateTime? ModifiedDate => _projectInfoProvider.ModifiedDate;

        public DateTime? LastAccessDate => _projectInfoProvider.LastAccessDate;

        public string? FileName => _projectInfoProvider.GetFilename();

        public string? FolderLocation => Path.GetDirectoryName(_projectInfoProvider.FilePath);

        public FileSize<double> FileSize { get; } = new FileSize<double>();

        public ICommand OpenFolderLocationCommand { get; }

        public PropertiesViewModel(ProjectInfoProvider projectInfoProvider)
        {
            _projectInfoProvider = projectInfoProvider;

            OpenFolderLocationCommand = CommandsManager.Create(() => IOHelper.OpenFolderLocation(projectInfoProvider.FilePath!), () => !string.IsNullOrEmpty(projectInfoProvider.FilePath));

            Disposables.AddRange(
            [
                _projectInfoProvider.WhenPropertyChanged(x => x.FilePath).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(FileName));
                    RaisePropertyChanged(nameof(FolderLocation));
                }),
                _projectInfoProvider.WhenPropertyChanged(x => x.CreatedDate).Subscribe(_ => RaisePropertyChanged(nameof(CreatedDate))),
                _projectInfoProvider.WhenPropertyChanged(x => x.ModifiedDate).Subscribe(_ => RaisePropertyChanged(nameof(ModifiedDate))),
                _projectInfoProvider.WhenPropertyChanged(x => x.LastAccessDate).Subscribe(_ => RaisePropertyChanged(nameof(LastAccessDate))),
                _projectInfoProvider.WhenPropertyChanged(x => x.FileSize).Subscribe(_ =>
                {
                    FileSize.Value = _projectInfoProvider.FileSize;
                    RaisePropertyChanged(nameof(FileSize));
                }),
            ]);
            _projectInfoProvider.WhenProjectChanged(x =>
            {
                CurrentProject = x;
                HasCurrentProject = x is not null;
            });
        }

        protected override string CreateTitle() => MyClubResources.Properties;

        protected override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }
    }
}
