// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal enum DatesSchedulingMethod
    {
        AsSoonAsPossible,

        Automatic,

        Manual,
    }

    internal class MatchdaysEditionParametersViewModel : EditableObject
    {
        private readonly UiObservableCollection<MatchdayViewModel> _availableMatchdays = [];
        private readonly Dictionary<DatesSchedulingMethod, IAddMatchdaysMethodViewModel> _datesSchedulingMethodViewModels = new()
        {
            { DatesSchedulingMethod.Manual, new MatchdaysEditionParametersManualViewModel() },
            { DatesSchedulingMethod.Automatic, new MatchdaysEditionParametersAutomaticViewModel() },
        };

        public MatchdaysEditionParametersViewModel() => AvailableMatchdays = new(_availableMatchdays);

        [IsRequired]
        [Display(Name = "Name", ResourceType = typeof(MyClubResources))]
        public string? NamePattern { get; set; }

        [IsRequired]
        [Display(Name = "ShortName", ResourceType = typeof(MyClubResources))]
        public string? ShortNamePattern { get; set; }

        [IsRequired]
        [Display(Name = nameof(DefaultTime), ResourceType = typeof(MyClubResources))]
        public TimeOnly? DefaultTime { get; set; }

        [IsRequired]
        [Display(Name = nameof(Index), ResourceType = typeof(MyClubResources))]
        public int Index { get; set; }

        [IsRequired]
        public DatesSchedulingMethod DatesSchedulingMethod { get; set; }

        [CanBeValidated(false)]
        public MatchdaysEditionParametersAutomaticViewModel AutomaticDatesSchedulingViewModel => (MatchdaysEditionParametersAutomaticViewModel)_datesSchedulingMethodViewModels[DatesSchedulingMethod.Automatic];

        [CanBeValidated(false)]
        public MatchdaysEditionParametersManualViewModel ManualDatesSchedulingViewModel => (MatchdaysEditionParametersManualViewModel)_datesSchedulingMethodViewModels[DatesSchedulingMethod.Manual];

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool DuplicationIsEnabled { get; set; }

        public MatchdayViewModel? DuplicationStart { get; set; }

        public bool InvertTeams { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<MatchdayViewModel> AvailableMatchdays { get; }

        public NewMatchdaysDto ToParametersDto(Guid? stageId) => new()
        {
            NamePattern = NamePattern,
            ShortNamePattern = ShortNamePattern,
            StartDuplicatedMatchday = DuplicationIsEnabled ? DuplicationStart?.Id : null,
            InvertTeams = InvertTeams,
            StartIndex = Index,
            StartTime = DefaultTime.GetValueOrDefault(),
            ScheduleVenues = true,
            StageId = stageId,
            DatesParameters = _datesSchedulingMethodViewModels[DatesSchedulingMethod].ProvideDatesParameters()
        };

        internal void Refresh(IMatchdaysStageViewModel stage)
        {
            _availableMatchdays.Set(stage.Matchdays.OrderBy(x => x.Date));
            DuplicationStart = AvailableMatchdays.FirstOrDefault();
        }

        internal void Reset(IMatchdaysStageViewModel stage)
        {
            DatesSchedulingMethod = DatesSchedulingMethod.Manual;
            NamePattern = MyClubResources.MatchdayNamePattern;
            ShortNamePattern = MyClubResources.MatchdayShortNamePattern;
            DefaultTime = stage.ProvideStartTime();
            DuplicationIsEnabled = false;
            InvertTeams = true;
            Index = stage.Matchdays.Count + 1;
            _datesSchedulingMethodViewModels.Values.ForEach(x => x.Reset(stage));
        }

        public override bool ValidateProperties()
        {
            if (!base.ValidateProperties()) return false;

            if (!_datesSchedulingMethodViewModels[DatesSchedulingMethod].ValidateProperties())
            {
                _datesSchedulingMethodViewModels[DatesSchedulingMethod].GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
                return false;
            }

            return true;
        }
    }
}
