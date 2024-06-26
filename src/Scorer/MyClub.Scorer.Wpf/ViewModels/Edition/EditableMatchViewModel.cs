﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Resources;
using MyNet.UI.Threading;
using MyNet.Utilities;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableMatchViewModel : EditableObject
    {
        private readonly ReadOnlyObservableCollection<TeamViewModel> _availableTeams;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Guid? Id { get; }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

        [IsRequired]
        [Display(Name = nameof(HomeTeam), ResourceType = typeof(MyClubResources))]
        public TeamViewModel? HomeTeam { get; set; }

        [IsRequired]
        [Display(Name = nameof(AwayTeam), ResourceType = typeof(MyClubResources))]
        [AlsoNotifyFor(nameof(HomeTeam))]
        public TeamViewModel? AwayTeam { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<TeamViewModel> AvailableTeams => _availableTeams;

        public StadiumSelectionViewModel StadiumSelection { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool IsReadOnly { get; set; } = false;

        public bool IsDeleting { get; set; }

        public EditableMatchViewModel(ISourceProvider<TeamViewModel> teamProviders, ISourceProvider<StadiumViewModel> stadiumsProvider, bool useHomeStadium = false)
            : this(null, teamProviders, stadiumsProvider, useHomeStadium) { }

        public EditableMatchViewModel(Guid? id, ISourceProvider<TeamViewModel> teamProviders, ISourceProvider<StadiumViewModel> stadiumsProvider, bool useHomeStadium = false)
        {
            Id = id;
            IsReadOnly = id.HasValue;
            StadiumSelection = new StadiumSelectionViewModel(stadiumsProvider);

            ValidationRules.Add<EditableMatchViewModel, TeamViewModel?>(x => x.HomeTeam, MessageResources.FieldXMustBeDifferentOfFieldYError.FormatWith(MyClubResources.HomeTeam, MyClubResources.AwayTeam), x => x is null || AwayTeam is null || x.Id != AwayTeam.Id);

            Disposables.Add(teamProviders.Connect().ObserveOn(Scheduler.UI).Bind(out _availableTeams).Subscribe());

            if (useHomeStadium)
                Disposables.Add(this.WhenPropertyChanged(x => x.HomeTeam, false).Subscribe(_ =>
                {
                    if (!IsReadOnly)
                        StadiumSelection.Select(HomeTeam?.Stadium?.Id);
                }));
        }

        internal bool IsValid() => Date.HasValue && Time.HasValue && HomeTeam is not null && AwayTeam is not null;

        public void InvertTeams()
        {
            if (IsReadOnly) return;

            using (ValidatePropertySuspender.Suspend())
                (HomeTeam, AwayTeam) = (AwayTeam, HomeTeam);
        }
    }
}
