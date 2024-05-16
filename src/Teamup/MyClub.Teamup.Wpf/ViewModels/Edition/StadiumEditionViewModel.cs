// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using MyNet.UI.ViewModels.Edition;
using MyNet.Observable.Attributes;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class StadiumEditionViewModel : EditionViewModel
    {
        public StadiumEditionViewModel(AddressService addressService) => Address = new EditableAddressViewModel(addressService);

        public EditableAddressViewModel Address { get; }

        public Guid? ItemId { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        [IsRequired]
        [Display(Name = nameof(Ground), ResourceType = typeof(MyClubResources))]
        public Ground Ground { get; set; }

        public void Load(EditableStadiumViewModel stadium)
        {
            ItemId = stadium.Id;
            Name = stadium.Name;
            Ground = stadium.Ground;
            Address.Load(stadium.Address);
        }

        public void New(string? name = null)
        {
            var defaultValue = StadiumService.NewStadium();
            ItemId = null;
            Name = !string.IsNullOrEmpty(name) ? name : defaultValue.Name;
            Ground = defaultValue.Ground;
            Address.Load(defaultValue.Address);
        }

        protected override void SaveCore() { }
    }
}
