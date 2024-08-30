// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.Settings;

namespace MyClub.Scorer.Wpf.ViewModels.Shell
{
    public class TimeAndLanguageViewModel : MyNet.UI.ViewModels.Shell.TimeAndLanguageViewModel
    {
        public override bool AutomaticTimeZone { get => TimeAndLanguageSettings.Default.AutomaticTimeZone; set => TimeAndLanguageSettings.Default.AutomaticTimeZone = value; }

        public override bool AutomaticCulture { get => TimeAndLanguageSettings.Default.AutomaticCulture; set => TimeAndLanguageSettings.Default.AutomaticCulture = value; }
    }
}
