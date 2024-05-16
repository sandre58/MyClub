// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Services;
using MyNet.Humanizer;
using MyNet.Observable;
using MyNet.UI.Busy;
using MyNet.UI.Commands;
using MyNet.UI.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableAddressViewModel : EditableObject
    {
        private readonly AddressService _addressService;

        public EditableAddressViewModel(AddressService addressService)
        {
            _addressService = addressService;
            BusyService = BusyManager.Create();

            GetFromClipboardCommand = CommandsManager.Create(GetFromClipboard, Clipboard.ContainsText);
            ComputeCoordinatesCommand = CommandsManager.Create(async () => await ComputeCoordinatesAsync().ConfigureAwait(false), () => Create() is not null);
        }

        public IBusyService BusyService { get; }

        [Display(Name = nameof(Address), ResourceType = typeof(MyClubResources))]
        public string? Address { get; set; }

        [MaxLength(5)]
        [Display(Name = nameof(PostalCode), ResourceType = typeof(MyClubResources))]
        public string? PostalCode { get; set; }

        [Display(Name = nameof(City), ResourceType = typeof(MyClubResources))]
        public string? City { get; set; }

        [Display(Name = nameof(Country), ResourceType = typeof(MyClubResources))]
        public Country? Country { get; set; }

        [Display(Name = nameof(Latitude), ResourceType = typeof(MyClubResources))]
        public double? Latitude { get; set; }

        [Display(Name = nameof(Longitude), ResourceType = typeof(MyClubResources))]
        public double? Longitude { get; set; }

        public ICommand GetFromClipboardCommand { get; }

        public ICommand ComputeCoordinatesCommand { get; }

        public Address? Create()
            => string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(PostalCode) && string.IsNullOrEmpty(City) && Country is null && !Latitude.HasValue && !Longitude.HasValue
                ? null
                : new Address(Address, PostalCode, City, Country, Latitude, Longitude);

        public void Load(Address? address)
        {
            Address = address?.Street;
            PostalCode = address?.PostalCode;
            City = address?.City;
            Country = address?.Country;
            Latitude = address?.Latitude;
            Longitude = address?.Longitude;
        }

        private async Task ComputeCoordinatesAsync()
        {
            if (Create() is Address address)
            {
                await BusyService.WaitIndeterminateAsync(() =>
                {
                    var coordinates = _addressService.GetCoordinatesFromAddress(address);

                    if (coordinates is not null)
                    {
                        Latitude = coordinates.Latitude;
                        Longitude = coordinates.Longitude;
                    }
                }).ConfigureAwait(false);
            }
        }

        private void GetFromClipboard()
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();

                var items = text.Split('-', StringSplitOptions.TrimEntries);

                if (items.Length >= 1)
                    Address = items[0].ToLowerCase().ToTitle();

                if (items.Length >= 2)
                {
                    var second = items[1].Split(" ");

                    if (second.Length >= 1)
                        PostalCode = second[0];

                    if (second.Length >= 2)
                        City = string.Join(" ", second.TakeLast(second.Length - 1)).ToLowerCase().ToTitle();
                }
            }
        }
    }
}
