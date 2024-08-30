// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData.Binding;
using MyNet.UI.Toasting;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Wpf.Presentation.Models;
using MyNet.Utilities;
using MyNet.Humanizer;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class TrainingSessionsEditionViewModel : EditionViewModel
    {
        private readonly TrainingSessionService _trainingSessionService;

        [CanSetIsModified(false)]
        public IReadOnlyCollection<TrainingSessionViewModel> Trainings { get; private set; } = new List<TrainingSessionViewModel>().AsReadOnly();

        [DoNotCheckEquality]
        public MultipleEditableValue<IEnumerable?> SelectedTeamIds { get; set; } = new();

        public MultipleEditableValue<TimeSpan?> StartTime { get; set; } = new();

        public MultipleEditableValue<TimeSpan?> EndTime { get; set; } = new();

        public MultipleEditableValue<string?> Place { get; set; } = new();

        public MultipleEditableValue<string?> Theme { get; set; } = new();

        [CanSetIsModified(false)]
        public TimeSpan MaxTime { get; private set; }

        [CanSetIsModified(false)]
        public TimeSpan MinTime { get; private set; }

        public TrainingSessionsEditionViewModel(TrainingSessionService trainingSessionService)
        {
            _trainingSessionService = trainingSessionService;
            Mode = ScreenMode.Edition;

            Disposables.AddRange(
            [
                StartTime.WhenAnyPropertyChanged().Subscribe(_ =>
                {
                    ValidateProperty(nameof(StartTime), StartTime);
                    ValidateProperty(nameof(EndTime), EndTime);
                }),
                EndTime.WhenAnyPropertyChanged().Subscribe(_ =>
                {
                    ValidateProperty(nameof(StartTime), StartTime);
                    ValidateProperty(nameof(EndTime), EndTime);
                }),
                Place.WhenAnyPropertyChanged().Subscribe(_ => ValidateProperty(nameof(Place), Place)),
                Theme.WhenAnyPropertyChanged().Subscribe(_ => ValidateProperty(nameof(Theme), Theme)),
                SelectedTeamIds.WhenAnyPropertyChanged().Subscribe(_ => ValidateProperty(nameof(SelectedTeamIds), SelectedTeamIds))
            ]);
            ValidationRules.Add<TrainingSessionsEditionViewModel, MultipleEditableValue<TimeSpan?>>(x => x.StartTime, MessageResources.FieldXIsRequiredError.FormatWith(MyClubResources.StartTime), x => x is not null && (!x.IsActive || x.Value is not null && x.Value != TimeSpan.MinValue));
            ValidationRules.Add<TrainingSessionsEditionViewModel, MultipleEditableValue<TimeSpan?>>(x => x.EndTime, MessageResources.FieldXIsRequiredError.FormatWith(MyClubResources.EndTime), x => x is not null && (!x.IsActive || x.Value is not null && x.Value != TimeSpan.MinValue));
            ValidationRules.Add<TrainingSessionsEditionViewModel, MultipleEditableValue<TimeSpan?>>(x => x.StartTime, MessageResources.FieldEndTimeMustBeUpperThanStartTimeError, x => x is null || !x.IsActive || !EndTime.IsActive || x.Value < EndTime.Value);
            ValidationRules.Add<TrainingSessionsEditionViewModel, MultipleEditableValue<TimeSpan?>>(x => x.EndTime, MessageResources.FieldEndTimeMustBeUpperThanStartTimeError, x => x is null || !x.IsActive || !StartTime.IsActive || x.Value > StartTime.Value);
            ValidationRules.Add<TrainingSessionsEditionViewModel, MultipleEditableValue<TimeSpan?>>(x => x.StartTime, MessageResources.FieldEndTimeMustBeUpperThanStartTimeError, x => x is null || !x.IsActive || x.IsActive && EndTime.IsActive || x.Value < MaxTime);
            ValidationRules.Add<TrainingSessionsEditionViewModel, MultipleEditableValue<TimeSpan?>>(x => x.EndTime, MessageResources.FieldEndTimeMustBeUpperThanStartTimeError, x => x is null || !x.IsActive || x.IsActive && StartTime.IsActive || x.Value > MinTime);
            ValidationRules.Add<TrainingSessionsEditionViewModel, MultipleEditableValue<IEnumerable?>>(x => x.SelectedTeamIds, MyClubResources.AnySelectedSquadsError, x => x is null || !x.IsActive || x.Value is not null && x.Value.Cast<Guid>().Any());
        }

        public void Load(IEnumerable<TrainingSessionViewModel> trainings)
        {
            Trainings = trainings.ToList().AsReadOnly();
            Reset();
        }

        protected override void ResetCore()
        {
            base.ResetCore();

            Theme.Reset(Trainings.Select(x => x.Theme));
            StartTime.Reset(Trainings.Select(x => (TimeSpan?)x.StartDate.ToLocalTime().TimeOfDay));
            EndTime.Reset(Trainings.Select(x => (TimeSpan?)x.EndDate.ToLocalTime().TimeOfDay));
            Place.Reset(Trainings.Select(x => x.Place));
            MaxTime = Trainings.Max(x => x.EndDate.TimeOfDay);
            MinTime = Trainings.Min(x => x.StartDate.TimeOfDay);

            if (Trainings.Count != 0)
            {
                var firstList = Trainings.First().Teams.Select(x => x.Id).OrderBy(x => x);
                var allEquals = Trainings.All(x => x.Teams.Select(x => x.Id).OrderBy(y => y).SequenceEqual(firstList));
                SelectedTeamIds.Reset(allEquals ? new List<IEnumerable?> { firstList } : Trainings.Select(x => x.Teams.Select(x => x.Id)));
            }
            else
            {
                SelectedTeamIds.Reset([]);
            }
        }

        #region Validate

        protected override void SaveCore()
        {
            if (!Place.IsActive && !Theme.IsActive && !StartTime.IsActive && !EndTime.IsActive && !SelectedTeamIds.IsActive) return;

            var result = _trainingSessionService.Update(Trainings.Select(x => x.Id).ToList(), new TrainingSessionMultipleDto
            {
                UpdatePlace = Place.IsActive,
                UpdateTheme = Theme.IsActive,
                EndDate = EndTime.GetActiveValue().HasValue ? DateTime.Today.ToUtc(EndTime.GetActiveValue()!.Value) : default,
                StartDate = StartTime.GetActiveValue().HasValue ? DateTime.Today.ToUtc(StartTime.GetActiveValue()!.Value) : default,
                Theme = Theme.GetActiveValue(),
                Place = Place.GetActiveValue(),
                TeamIds = SelectedTeamIds.GetActiveValue() is not null && SelectedTeamIds.GetActiveValue()!.OfType<Guid>().Any() ? SelectedTeamIds.GetActiveValue()!.OfType<Guid>().ToList() : null
            });

            ToasterManager.ShowSuccess(nameof(MessageResources.XItemsHasBeenModifiedSuccess).TranslateAndFormatWithCount(result.Count));
        }

        #endregion
    }
}
