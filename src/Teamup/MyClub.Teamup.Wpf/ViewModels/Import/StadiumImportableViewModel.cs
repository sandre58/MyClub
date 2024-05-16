// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyNet.Observable.Attributes;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class StadiumImportableViewModel : ImportableViewModel
    {
        public StadiumImportableViewModel(string name, ImportMode mode = ImportMode.Add, bool import = false) : base(mode, import) => Name = name;

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; }

        public Ground Ground { get; set; }

        [Display(Name = nameof(Address), ResourceType = typeof(MyClubResources))]
        public string? Address { get; set; }

        [MaxLength(5)]
        [Display(Name = nameof(PostalCode), ResourceType = typeof(MyClubResources))]
        public string? PostalCode { get; set; }

        [Display(Name = nameof(City), ResourceType = typeof(MyClubResources))]
        public string? City { get; set; }

        [Display(Name = nameof(City), ResourceType = typeof(MyClubResources))]
        public Country? Country { get; set; }

        [Display(Name = nameof(Latitude), ResourceType = typeof(MyClubResources))]
        public double? Latitude { get; set; }

        [Display(Name = nameof(Longitude), ResourceType = typeof(MyClubResources))]
        public double? Longitude { get; set; }

        public Address? GetAddress()
            => !string.IsNullOrEmpty(Address) || !string.IsNullOrEmpty(PostalCode) || !string.IsNullOrEmpty(City) || Country is not null || Longitude.HasValue || Latitude.HasValue
                ? new Address(Address, PostalCode, City, Country, Latitude, Longitude)
                : null;

        public override string ToString() => string.Join(", ", new[] { Name, City }.NotNull());
    }
}
