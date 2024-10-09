// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Wpf.ViewModels.TeamsPage;
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
        private readonly ReadOnlyObservableCollection<IVirtualTeamViewModel> _availableTeams;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Guid? Id { get; }

        public EditableDateTime CurrentDate { get; set; } = new();

        [IsRequired]
        [Display(Name = nameof(HomeTeam), ResourceType = typeof(MyClubResources))]
        public IVirtualTeamViewModel? HomeTeam { get; set; }

        [IsRequired]
        [Display(Name = nameof(AwayTeam), ResourceType = typeof(MyClubResources))]
        [AlsoNotifyFor(nameof(HomeTeam))]
        public IVirtualTeamViewModel? AwayTeam { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<IVirtualTeamViewModel> AvailableTeams => _availableTeams;

        public StadiumSelectionViewModel StadiumSelection { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool IsReadOnly { get; set; } = false;

        public bool IsDeleting { get; set; }

        public EditableMatchViewModel(Guid? id, IEnumerable<IVirtualTeamViewModel> teams, ISourceProvider<StadiumViewModel> stadiumsProvider, bool useHomeStadium = false)
            : this(id, new ItemsSourceProvider<IVirtualTeamViewModel>(teams), stadiumsProvider, useHomeStadium) { }

        public EditableMatchViewModel(IEnumerable<IVirtualTeamViewModel> teams, ISourceProvider<StadiumViewModel> stadiumsProvider, bool useHomeStadium = false)
            : this(null, new ItemsSourceProvider<IVirtualTeamViewModel>(teams), stadiumsProvider, useHomeStadium) { }

        public EditableMatchViewModel(ISourceProvider<IVirtualTeamViewModel> teamProviders, ISourceProvider<StadiumViewModel> stadiumsProvider, bool useHomeStadium = false)
            : this(null, teamProviders, stadiumsProvider, useHomeStadium) { }

        public EditableMatchViewModel(Guid? id, ISourceProvider<IVirtualTeamViewModel> teamProviders, ISourceProvider<StadiumViewModel> stadiumsProvider, bool useHomeStadium = false)
        {
            Id = id;
            IsReadOnly = id.HasValue;
            StadiumSelection = new StadiumSelectionViewModel(stadiumsProvider);

            ValidationRules.Add<EditableMatchViewModel, IVirtualTeamViewModel?>(x => x.HomeTeam, MessageResources.FieldXMustBeDifferentOfFieldYError.FormatWith(MyClubResources.HomeTeam, MyClubResources.AwayTeam), x => x is null || AwayTeam is null || x.Id != AwayTeam.Id);

            Disposables.Add(teamProviders.Connect().ObserveOn(Scheduler.UI).Bind(out _availableTeams).Subscribe());

            if (useHomeStadium)
                Disposables.Add(this.WhenPropertyChanged(x => x.HomeTeam, false).Subscribe(_ =>
                {
                    if (!IsReadOnly && HomeTeam is TeamViewModel homeTeam)
                        StadiumSelection.Select(homeTeam.Stadium?.Id);
                }));
        }

        internal bool IsValid() => CurrentDate.HasValue && HomeTeam is not null && AwayTeam is not null;

        public void InvertTeams()
        {
            if (IsReadOnly) return;

            using (ValidatePropertySuspender.Suspend())
                (HomeTeam, AwayTeam) = (AwayTeam, HomeTeam);
        }
    }
}
