// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reactive.Disposables;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.UI.Helpers;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;

namespace MyClub.Scorer.Wpf.ViewModels.Shell
{
    internal class PropertiesViewModel : WorkspaceViewModel
    {
        private readonly ProjectInfoProvider _projectInfoProvider;
        private CompositeDisposable _projectDisposables = [];

        public bool ProjectIsLoaded { get; private set; }

        public string? Name { get; private set; }

        public string? ProjectCreatedBy { get; private set; }

        public DateTime? ProjectCreatedUtcDate { get; private set; }

        public string? ProjectModifiedBy { get; private set; }

        public DateTime? ProjectModifiedUtcDate { get; private set; }

        public DateTime? FileCreatedUtcDate { get; private set; }

        public DateTime? FileModifiedUtcDate { get; private set; }

        public DateTime? FileLastAccessUtcDate { get; private set; }

        public string? FileName { get; private set; }

        public string? FolderLocation { get; private set; }

        public FileSize<double> FileSize { get; } = new FileSize<double>();

        public ICommand OpenFolderLocationCommand { get; }

        public PropertiesViewModel(ProjectInfoProvider projectInfoProvider)
        {
            _projectInfoProvider = projectInfoProvider;
            OpenFolderLocationCommand = CommandsManager.Create(() => IOHelper.OpenFolderLocation(projectInfoProvider.FilePath!), () => !string.IsNullOrEmpty(projectInfoProvider.FilePath));

            Disposables.AddRange(
            [
                projectInfoProvider.WhenPropertyChanged(x => x.FilePath).Subscribe(x =>
                {
                    FileName = projectInfoProvider.GetFilename();
                    FolderLocation = Path.GetDirectoryName(x.Value);
                }),
                projectInfoProvider.WhenPropertyChanged(x => x.IsLoaded).Subscribe(x => ProjectIsLoaded = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.Name).Subscribe(x => Name = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.CreatedDate).Subscribe(x => FileCreatedUtcDate = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.ModifiedDate).Subscribe(x => FileModifiedUtcDate = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.LastAccessDate).Subscribe(x => FileLastAccessUtcDate = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.FileSize).Subscribe(_ =>
                {
                    FileSize.Value = projectInfoProvider.FileSize;
                    RaisePropertyChanged(nameof(FileSize));
                }),
            ]);

            projectInfoProvider.UnloadRunner.RegisterOnEnd(this, () =>
            {
                _projectDisposables.Dispose();
                UpdateProjectInfo(null);
            });

            projectInfoProvider.LoadRunner.RegisterOnEnd(this, x => _projectDisposables = new(x.WhenAnyPropertyChanged(nameof(IProject.CreatedBy), nameof(IProject.CreatedAt), nameof(IProject.ModifiedBy), nameof(IProject.ModifiedAt)).Subscribe(_ => UpdateProjectInfo(x))));
        }

        protected override string CreateTitle() => MyClubResources.Properties;

        private void UpdateProjectInfo(IProject? project)
        {
            ProjectCreatedBy = project?.CreatedBy;
            ProjectCreatedUtcDate = project?.CreatedAt;
            ProjectModifiedBy = project?.ModifiedBy;
            ProjectModifiedUtcDate = project?.ModifiedAt;
        }

        protected override void Cleanup()
        {
            _projectInfoProvider.LoadRunner.Unregister(this);
            _projectInfoProvider.UnloadRunner.Unregister(this);
            _projectDisposables.Dispose();
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }
    }
}
