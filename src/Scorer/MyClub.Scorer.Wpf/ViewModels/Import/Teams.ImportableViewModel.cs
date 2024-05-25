// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using MyClub.CrossCutting.Localization;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class TeamImportableViewModel : ImportableViewModel
    {
        public TeamImportableViewModel(string name, ImportMode mode = ImportMode.Add, bool import = false) : base(mode, import) => Name = name;

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; }

        public byte[]? Logo { get; set; }

        public Color? HomeColor { get; set; }

        public Color? AwayColor { get; set; }

        public StadiumImportableViewModel? Stadium { get; set; }

        public Country? Country { get; set; }
    }
}
