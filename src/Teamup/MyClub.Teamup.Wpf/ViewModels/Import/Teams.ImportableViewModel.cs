// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class TeamImportableViewModel : ImportableViewModel
    {
        public TeamImportableViewModel(string clubName, ImportMode mode = ImportMode.Add, bool import = false) : base(mode, import) => ClubName = clubName;

        [Display(Name = nameof(ClubName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ClubName { get; }

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; set; } = string.Empty;

        public Category? Category { get; set; }

        public byte[]? Logo { get; set; }

        public Color? HomeColor { get; set; }

        public Color? AwayColor { get; set; }

        public StadiumImportableViewModel? Stadium { get; set; }

        public Country? Country { get; set; }
    }
}
