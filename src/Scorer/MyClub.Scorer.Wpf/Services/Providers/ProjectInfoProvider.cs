// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Messages;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.Messages;
using MyNet.DynamicData.Extensions;
using MyNet.Observable;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Suspending;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    public sealed class ProjectInfoProvider : ObservableObject
    {
        private readonly Subject<IProject> _projectLoadedSubject = new();
        private readonly Subject<bool> _projectClosingSubject = new();
        private readonly Suspender _isDirtySuspender = new();
        private CompositeDisposable? _projectDisposables;

        public string? Name { get; private set; }

        public byte[]? Image { get; private set; }

        public string? FilePath { get; private set; }

        public double? FileSize { get; private set; }

        public DateTime? CreatedDate { get; private set; }

        public DateTime? ModifiedDate { get; private set; }

        public DateTime? LastAccessDate { get; private set; }

        public bool IsDirty { get; private set; }

        public bool IsLoaded { get; private set; }

        public ProjectInfoProvider()
        {
            Messenger.Default.Register<CurrentProjectLoadedMessage>(this, OnCurrentProjectLoaded);
            Messenger.Default.Register<CurrentProjectCloseRequestMessage>(this, OnCurrentProjectCloseRequest);
            Messenger.Default.Register<CurrentProjectSavedMessage>(this, OnCurrentProjectSaved);
        }

        [SuppressPropertyChangedWarnings]
        private void OnCurrentProjectCloseRequest(CurrentProjectCloseRequestMessage message)
        {
            _projectClosingSubject.OnNext(true);
            ClearProject();
            ResetFileInfo();
            SetIsDirty(false);
        }

        [SuppressPropertyChangedWarnings]
        private void OnCurrentProjectLoaded(CurrentProjectLoadedMessage message)
        {
            var currentProject = message.Project;

            using (_isDirtySuspender.Suspend())
            {
                _projectDisposables = new(
                    currentProject.WhenPropertyChanged(x => x.Name).Subscribe(x => Name = x.Value),
                    currentProject.WhenPropertyChanged(x => x.Image).Subscribe(x => Image = x.Value),
                    currentProject.Teams.ToObservableChangeSet(x => x.Id).SubscribeMany(x => x.Players.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true))).SubscribeAll(() => SetIsDirty(true)),
                    currentProject.Teams.ToObservableChangeSet(x => x.Id).SubscribeMany(x => x.Staff.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true))).Subscribe(),
                    currentProject.Stadiums.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true)),
                    currentProject.Competition.WhenAnyPropertyChanged().Subscribe(_ => SetIsDirty(true)),
                    currentProject.WhenAnyPropertyChanged().Subscribe(_ => SetIsDirty(true))
                );

                switch (currentProject.Competition)
                {
                    case League league:
                        _projectDisposables.Add(league.Matchdays.ToObservableChangeSet().SubscribeMany(x => x.Matches.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true))).SubscribeAll(() => SetIsDirty(true)));
                        break;
                    default:
                        break;
                }
            }

            // New project or project from template
            if (string.IsNullOrEmpty(message.Filename))
            {
                ResetFileInfo();
                SetIsDirty(true);
            }

            // Open a project
            else
            {
                SetFileInfo(message.Filename);
                SetIsDirty(false);
            }

            IsLoaded = true;
            _projectLoadedSubject.OnNext(currentProject);
        }

        private void OnCurrentProjectSaved(CurrentProjectSavedMessage message)
        {
            SetFileInfo(message.FilePath);
            SetIsDirty(false);
        }

        private void ClearProject()
        {
            _projectDisposables?.Dispose();
            IsLoaded = false;
            Name = null;
            Image = null;
        }

        public string? GetFilename(bool withExtension = true) => withExtension ? Path.GetFileName(FilePath) : Path.GetFileNameWithoutExtension(FilePath);

        public string ProvideExportName(string name) => $"{Name?.ToFilename()} - {name} - {DateTime.Now:yyyy-MM-dd HH-mm-ss}";

        private void ResetFileInfo()
        {
            FilePath = null;
            FileSize = null;
            CreatedDate = null;
            ModifiedDate = null;
            LastAccessDate = null;
        }

        private void SetFileInfo(string filepath)
        {
            FilePath = filepath.IsRequiredOrThrow();

            var fileInfo = new FileInfo(FilePath);

            if (fileInfo.Exists)
            {
                FileSize = fileInfo.Length;
                CreatedDate = fileInfo.CreationTimeUtc;
                ModifiedDate = fileInfo.LastWriteTimeUtc;
                LastAccessDate = fileInfo.LastAccessTimeUtc;
            }
        }

        private void SetIsDirty(bool value)
        {
            if (!_isDirtySuspender.IsSuspended && IsDirty != value)
            {
                IsDirty = value;
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();

            _projectLoadedSubject.Dispose();
            ClearProject();
            Messenger.Default.Unregister(this);
        }

        internal void WhenProjectLoaded(Action<IProject> action) => _projectLoadedSubject.Subscribe(action);

        internal void WhenProjectClosing(Action action) => _projectClosingSubject.Subscribe(_ => action());
    }
}
