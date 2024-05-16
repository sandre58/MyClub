// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using DynamicData.Binding;
using MyNet.Utilities;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableMatchViewModel : EditableObject
    {
        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan Time { get; set; }

        public EditableTeamSelectionViewModel HomeTeamSelection { get; }

        public EditableTeamSelectionViewModel AwayTeamSelection { get; }

        public EditableStadiumSelectionViewModel StadiumSelection { get; }

        public EditableMatchViewModel(StadiumsProvider stadiumsProvider, StadiumPresentationService stadiumPresentationService, TeamPresentationService teamPresentationService)
        {
            StadiumSelection = new EditableStadiumSelectionViewModel(stadiumsProvider, stadiumPresentationService);
            HomeTeamSelection = new EditableTeamSelectionViewModel(teamPresentationService);
            AwayTeamSelection = new EditableTeamSelectionViewModel(teamPresentationService);

            ValidationRules.AddNotNull<EditableMatchViewModel, EditableTeamSelectionViewModel>(x => x.HomeTeamSelection, MessageResources.FieldXIsRequiredError.FormatWith(MyClubResources.HomeTeam), x => !(x.SelectedItem is null && AwayTeamSelection.SelectedItem is not null));
            ValidationRules.AddNotNull<EditableMatchViewModel, EditableTeamSelectionViewModel>(x => x.AwayTeamSelection, MessageResources.FieldXIsRequiredError.FormatWith(MyClubResources.AwayTeam), x => !(x.SelectedItem is null && HomeTeamSelection.SelectedItem is not null));
            ValidationRules.AddNotNull<EditableMatchViewModel, EditableTeamSelectionViewModel>(x => x.HomeTeamSelection, MessageResources.FieldXMustBeDifferentOfFieldYError.FormatWith(MyClubResources.HomeTeam, MyClubResources.AwayTeam), x => x.SelectedItem is null || x.SelectedItem?.Id != AwayTeamSelection.SelectedItem?.Id);

            Disposables.AddRange(
            [
                HomeTeamSelection.WhenPropertyChanged(x => x.SelectedItem).Subscribe(_ => StadiumSelection.Select(HomeTeamSelection.SelectedItem?.Stadium?.Id))
            ]);
        }
    }
}
