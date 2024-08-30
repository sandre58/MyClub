// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using MyClub.CrossCutting.Localization;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableTeamEditionViewModel : EditionViewModel
    {
        public EditableTeamEditionViewModel(IEnumerable<EditableStadiumViewModel> availableStadiums) => AvailableStadiums = new(new(availableStadiums));

        private EditableTeamViewModel? _originalTeam;

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; set; } = string.Empty;

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        public byte[]? Logo { get; set; }

        public Color? HomeColor { get; set; }

        public Color? AwayColor { get; set; }

        public Country? Country { get; set; }

        public EditableStadiumViewModel? Stadium { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public ReadOnlyObservableCollection<EditableStadiumViewModel> AvailableStadiums { get; set; }

        public void Load(EditableTeamViewModel team)
        {
            Mode = MyNet.UI.ViewModels.ScreenMode.Edition;

            _originalTeam = team;
        }

        public void New(string name)
        {
            Mode = MyNet.UI.ViewModels.ScreenMode.Creation;

            _originalTeam = new EditableTeamViewModel()
            {
                Name = name,
                ShortName = name.GetInitials(),
                HomeColor = RandomGenerator.Color().ToColor(),
                AwayColor = RandomGenerator.Color().ToColor()
            };
        }

        protected override void RefreshCore()
        {
            Name = (_originalTeam?.Name).OrEmpty();
            ShortName = (_originalTeam?.ShortName).OrEmpty();
            Logo = _originalTeam?.Logo;
            AwayColor = _originalTeam?.AwayColor;
            HomeColor = _originalTeam?.HomeColor;
            Country = _originalTeam?.Country;
            Stadium = _originalTeam?.Stadium;
        }

        protected override void SaveCore() { }
    }
}
