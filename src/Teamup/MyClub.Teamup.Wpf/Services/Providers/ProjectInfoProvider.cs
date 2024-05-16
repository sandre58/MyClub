// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.DynamicData.Extensions;
using MyNet.Utilities.Messaging;
using MyNet.Observable;
using MyNet.Utilities.Suspending;
using MyClub.Teamup.Application.Messages;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Messages;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    public sealed class ProjectInfoProvider : ObservableObject
    {
        private Project? _currentProject;
        private readonly Subject<string?> _clubPropertyChanged = new();
        private readonly Subject<Project?> _projectChangedSubject = new();
        private readonly Subject<Project?> _projectChangingSubject = new();
        private readonly Suspender _isDirtySuspender = new();
        private CompositeDisposable? _projectDisposables;

        public string? Name { get; private set; }

        public Color Color => _currentProject?.Color.ToColor() ?? default;

        public byte[]? Image => _currentProject?.Image;

        public string? FilePath { get; private set; }

        public double? FileSize { get; private set; }

        public DateTime? CreatedDate { get; private set; }

        public DateTime? ModifiedDate { get; private set; }

        public DateTime? LastAccessDate { get; private set; }

        public bool IsDirty { get; private set; }

        public ProjectInfoProvider()
        {
            Messenger.Default.Register<CurrentProjectChangedMessage>(this, OnCurrentProjectChanged);
            Messenger.Default.Register<CurrentProjectChangingMessage>(this, OnCurrentProjectChanging);
            Messenger.Default.Register<CurrentProjectSavedMessage>(this, OnCurrentProjectSaved);
            Messenger.Default.Register<StadiumsChangedMessage>(this, _ => SetIsDirty(true));
        }

        public Project? GetCurrentProject() => _currentProject;

        [SuppressPropertyChangedWarnings]
        private void OnCurrentProjectChanging(CurrentProjectChangingMessage message)
        {
            ClearProject();

            _projectChangingSubject.OnNext(message.Project);
        }

        [SuppressPropertyChangedWarnings]
        private void OnCurrentProjectChanged(CurrentProjectChangedMessage message)
        {
            // Close project
            if (message.CurrentProject is null)
            {
                ResetFileInfo();
                SetIsDirty(false);
                _projectChangedSubject.OnNext(null);
                return;
            }

            _currentProject = message.CurrentProject;

            using (_isDirtySuspender.Suspend())
            {
                _projectDisposables = new(
                    Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(x => _currentProject.Club.PropertyChanged += x, x => _currentProject.Club.PropertyChanged -= x).Subscribe(x =>
                    {
                        SetIsDirty(true);
                        _clubPropertyChanged.OnNext(x.EventArgs.PropertyName);
                    }),
                    _currentProject.WhenPropertyChanged(x => x.Name).Subscribe(x => Name = x.Value),
                    _currentProject.WhenPropertyChanged(x => x.Color).Subscribe(_ => RaisePropertyChanged(nameof(Color))),
                    _currentProject.WhenPropertyChanged(x => x.Image).Subscribe(_ => RaisePropertyChanged(nameof(Image))),
                    _currentProject.WhenPropertyChanged(x => x.MainTeam).Subscribe(_ => Messenger.Default.Send(new MainTeamChangedMessage(_currentProject.GetMainTeams().AsReadOnly()))),
                    _currentProject.WhenAnyPropertyChanged().Subscribe(_ => SetIsDirty(true)),
                    _currentProject.Holidays.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true)),
                    _currentProject.SendedMails.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true)),
                    _currentProject.Tactics.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true)),
                    _currentProject.Players.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true)),
                    _currentProject.Players.ToObservableChangeSet(x => x.Id).SubscribeMany(x => x.Player.Injuries.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true))).Subscribe(),
                    _currentProject.Players.ToObservableChangeSet(x => x.Id).SubscribeMany(x => x.Player.Absences.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true))).Subscribe(),
                    _currentProject.Cycles.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true)),
                    _currentProject.Competitions.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true)),
                    _currentProject.TrainingSessions.ToObservableChangeSet(x => x.Id).SubscribeAll(() => SetIsDirty(true))
                );
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

            _projectChangedSubject.OnNext(_currentProject);
        }

        private void OnCurrentProjectSaved(CurrentProjectSavedMessage message)
        {
            SetFileInfo(message.FilePath);
            SetIsDirty(false);
        }

        private void ClearProject()
        {
            _projectDisposables?.Dispose();
            _currentProject = null;
        }

        public string? GetFilename(bool withExtension = true) => withExtension ? Path.GetFileName(FilePath) : Path.GetFileNameWithoutExtension(FilePath);

        public string ProvideExportName(string name) => $"{_currentProject?.Name?.ToFilename()} - {name} - {DateTime.Now:yyyy-MM-dd HH-mm-ss}";

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

            _projectChangedSubject.Dispose();
            ClearProject();
            Messenger.Default.Unregister(this);
        }

        internal void WhenProjectChanged(Action<Project?> action) => _projectChangedSubject.Subscribe(x => action(x));

        internal void WhenClubPropertyChanged<T>(Expression<Func<Club, T>> propertyExpression, Action<T> action)
            => _clubPropertyChanged.Subscribe(x =>
            {
                if (_currentProject is null) return;

                var propertyName = propertyExpression.GetPropertyName();

                if (x == propertyName)
                {
                    var func = propertyExpression.Compile();
                    action(func(_currentProject.Club));
                }
            });

        internal void WhenProjectChanging(Action<Project?> action) => _projectChangingSubject.Subscribe(x => action(x));
    }
}
