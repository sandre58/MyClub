// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Services;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableStadiumEditionViewModel : EditionViewModel
    {
        private EditableStadiumViewModel? _originalStadium;

        public EditableStadiumEditionViewModel(AddressService addressService) => Address = new EditableAddressViewModel(addressService);

        public EditableAddressViewModel Address { get; }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        [IsRequired]
        [Display(Name = nameof(Ground), ResourceType = typeof(MyClubResources))]
        public Ground Ground { get; set; }

        public void Load(EditableStadiumViewModel stadium)
        {
            Mode = MyNet.UI.ViewModels.ScreenMode.Edition;

            _originalStadium = stadium;
        }

        public void New(string? name = null)
        {
            Mode = MyNet.UI.ViewModels.ScreenMode.Creation;

            _originalStadium = new EditableStadiumViewModel()
            {
                Name = name.OrEmpty()
            };
        }

        protected override void RefreshCore()
        {
            Name = (_originalStadium?.Name).OrEmpty();
            Ground = _originalStadium?.Ground ?? Ground.Grass;
            Address.Load(_originalStadium?.Address);
        }

        protected override void SaveCore() { }
    }
}
