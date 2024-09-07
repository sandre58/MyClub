// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
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
        private readonly ReadOnlyObservableCollection<ITeamViewModel> _availableTeams;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Guid? Id { get; }

        public EditableDateTime CurrentDate { get; set; } = new();

        [IsRequired]
        [Display(Name = nameof(HomeTeam), ResourceType = typeof(MyClubResources))]
        public ITeamViewModel? HomeTeam { get; set; }

        [IsRequired]
        [Display(Name = nameof(AwayTeam), ResourceType = typeof(MyClubResources))]
        [AlsoNotifyFor(nameof(HomeTeam))]
        public ITeamViewModel? AwayTeam { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<ITeamViewModel> AvailableTeams => _availableTeams;

        public StadiumSelectionViewModel StadiumSelection { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool IsReadOnly { get; set; } = false;

        public bool IsDeleting { get; set; }

        public EditableMatchViewModel(ISourceProvider<ITeamViewModel> teamProviders, ISourceProvider<IStadiumViewModel> stadiumsProvider, bool useHomeStadium = false)
            : this(null, teamProviders, stadiumsProvider, useHomeStadium) { }

        public EditableMatchViewModel(Guid? id, ISourceProvider<ITeamViewModel> teamProviders, ISourceProvider<IStadiumViewModel> stadiumsProvider, bool useHomeStadium = false)
        {
            Id = id;
            IsReadOnly = id.HasValue;
            StadiumSelection = new StadiumSelectionViewModel(stadiumsProvider);

            ValidationRules.Add<EditableMatchViewModel, ITeamViewModel?>(x => x.HomeTeam, MessageResources.FieldXMustBeDifferentOfFieldYError.FormatWith(MyClubResources.HomeTeam, MyClubResources.AwayTeam), x => x is null || AwayTeam is null || x.Id != AwayTeam.Id);

            Disposables.Add(teamProviders.Connect().ObserveOn(Scheduler.UI).Bind(out _availableTeams).Subscribe());

            if (useHomeStadium)
                Disposables.Add(this.WhenPropertyChanged(x => x.HomeTeam, false).Subscribe(_ =>
                {
                    if (!IsReadOnly)
                        StadiumSelection.Select(HomeTeam?.Stadium?.Id);
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
