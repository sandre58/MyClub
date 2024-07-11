// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Wpf.Services.Deferrers;
using MyNet.Observable.Attributes;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class StadiumEditionViewModel : EntityEditionViewModel<Stadium, StadiumDto, StadiumService>, IEntityEditionViewModel
    {
        private readonly StadiumsChangedDeferrer _stadiumsChangedDeferrer;

        public StadiumEditionViewModel(StadiumService service, AddressService addressService, StadiumsChangedDeferrer stadiumsChangedDeferrer)
            : base(service)
        {
            _stadiumsChangedDeferrer = stadiumsChangedDeferrer;
            Address = new EditableAddressViewModel(addressService);
        }

        public EditableAddressViewModel Address { get; }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string Name { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(Ground), ResourceType = typeof(MyClubResources))]
        public Ground Ground { get; set; }


        protected override StadiumDto ToDto() => new()
        {
            Id = ItemId,
            Name = Name,
            Ground = Ground,
            Address = Address.Create(),
        };

        protected override void RefreshFrom(Stadium item)
        {
            Name = item.Name.OrEmpty();
            Ground = item.Ground;
            Address.Load(item.Address);
        }

        protected override void ResetItem()
        {
            var defaultValues = StadiumService.NewStadium();
            Name = defaultValues.Name.OrEmpty();
            Ground = defaultValues.Ground;
            Address.Load(defaultValues.Address);
        }

        protected override void SaveCore()
        {
            using (_stadiumsChangedDeferrer.Defer())
                base.SaveCore();
        }
    }
}
