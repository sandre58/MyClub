// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using MyClub.Scorer.Wpf.Settings;
using MyNet.UI.Services;
using MyNet.Utilities.Localization;

namespace MyClub.Scorer.Wpf.Services
{
    internal sealed class TimeAndLanguageSettingsService : IPersistentSettingsService
    {
        public void Reload()
        {
            if (TimeAndLanguageSettings.Default.AutomaticCulture)
                GlobalizationService.Current.SetCulture(CultureInfo.InstalledUICulture.Name);
            else if (!string.IsNullOrEmpty(TimeAndLanguageSettings.Default.Language))
                GlobalizationService.Current.SetCulture(TimeAndLanguageSettings.Default.Language);

            if (TimeAndLanguageSettings.Default.AutomaticTimeZone)
                GlobalizationService.Current.SetTimeZone(TimeZoneInfo.Local);
            else if (!string.IsNullOrEmpty(TimeAndLanguageSettings.Default.TimeZone))
                GlobalizationService.Current.SetTimeZone(TimeZoneInfo.FindSystemTimeZoneById(TimeAndLanguageSettings.Default.TimeZone));
        }

        public void Reset()
        {
            GlobalizationService.Current.SetCulture(CultureInfo.InstalledUICulture.Name);
            GlobalizationService.Current.SetTimeZone(TimeZoneInfo.Local);
        }

        public void Save()
        {
            TimeAndLanguageSettings.Default.Language = !TimeAndLanguageSettings.Default.AutomaticCulture ? GlobalizationService.Current.Culture.Name : string.Empty;
            TimeAndLanguageSettings.Default.TimeZone = !TimeAndLanguageSettings.Default.AutomaticTimeZone ? GlobalizationService.Current.TimeZone.Id : string.Empty;
            TimeAndLanguageSettings.Default.Save();
        }
    }
}
