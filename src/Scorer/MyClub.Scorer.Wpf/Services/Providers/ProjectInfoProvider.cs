// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Messages;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable;
using MyNet.Observable.Deferrers;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Suspending;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal sealed class ProjectInfoProvider : ObservableObject
    {
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

        public ActionRunner UnloadRunner { get; }

        public ActionRunner<(IProject project, string? filename), IProject> LoadRunner { get; }

        public ProjectPreferencesViewModel Preferences { get; private set; } = new();

        public ProjectInfoProvider()
        {
            UnloadRunner = new(() =>
            {
                ClearProject();
                ResetFileInfo();
                SetIsDirty(false);
            });
            LoadRunner = new(x =>
            {
                using (_isDirtySuspender.Suspend())
                {
                    Preferences.Load(x.project.Preferences);
                    _projectDisposables = new(
                        x.project.WhenPropertyChanged(y => y.Name).Subscribe(y => Name = y.Value),
                        x.project.WhenPropertyChanged(y => y.Image).Subscribe(y => Image = y.Value),
                        x.project.Teams.ToObservableChangeSet(y => y.Id).SubscribeMany(y => y.Players.ToObservableChangeSet(y => y.Id).SubscribeAll(() => SetIsDirty(true))).SubscribeAll(() => SetIsDirty(true)),
                        x.project.Teams.ToObservableChangeSet(y => y.Id).SubscribeMany(x => x.Staff.ToObservableChangeSet(y => y.Id).SubscribeAll(() => SetIsDirty(true))).Subscribe(),
                        x.project.Stadiums.ToObservableChangeSet(y => y.Id).SubscribeAll(() => SetIsDirty(true)),
                        x.project.Competition.WhenAnyPropertyChanged().Subscribe(_ => SetIsDirty(true)),
                        x.project.WhenAnyPropertyChanged().Subscribe(_ => SetIsDirty(true)),
                        x.project.Preferences.WhenAnyPropertyChanged().Subscribe(_ => SetIsDirty(true))
                    );

                    switch (x.project.Competition)
                    {
                        case League league:
                            _projectDisposables.Add(league.Matchdays.ToObservableChangeSet().SubscribeMany(x => x.Matches.ToObservableChangeSet(y => y.Id).SubscribeAll(() => SetIsDirty(true))).SubscribeAll(() => SetIsDirty(true)));
                            break;
                        default:
                            break;
                    }
                }

                // New project or project from template
                if (string.IsNullOrEmpty(x.filename))
                {
                    ResetFileInfo();
                    SetIsDirty(true);
                }

                // Open a project
                else
                {
                    SetFileInfo(x.filename);
                    SetIsDirty(false);
                }

                IsLoaded = true;
            }, true);
            LoadRunner.RegisterOnEnd(this, x => LogManager.Trace($"{GetType().Name} : Load Project '{x.Name}' in {LoadRunner.LastTimeElapsed.Milliseconds}ms"));

            Messenger.Default.Register<CurrentProjectLoadedMessage>(this, OnCurrentProjectLoaded);
            Messenger.Default.Register<CurrentProjectCloseRequestMessage>(this, OnCurrentProjectCloseRequest);
            Messenger.Default.Register<CurrentProjectSavedMessage>(this, OnCurrentProjectSaved);
        }

        [SuppressPropertyChangedWarnings]
        private void OnCurrentProjectCloseRequest(CurrentProjectCloseRequestMessage message) => UnloadRunner.Run();

        [SuppressPropertyChangedWarnings]
        private void OnCurrentProjectLoaded(CurrentProjectLoadedMessage message) => LoadRunner.Run((message.Project, message.Filename), () => message.Project);

        private void OnCurrentProjectSaved(CurrentProjectSavedMessage message)
        {
            SetFileInfo(message.FilePath);
            SetIsDirty(false);
        }

        private void ClearProject()
        {
            _projectDisposables?.Dispose();
            Preferences.Clear();
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

            LoadRunner.Dispose();
            UnloadRunner.Dispose();
            Preferences.Dispose();
            ClearProject();
            Messenger.Default.Unregister(this);
        }
    }
}
