// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using MyClub.Teamup.Wpf.Settings;
using MyNet.UI.Services;
using MyNet.Utilities.Localization;

namespace MyClub.Teamup.Wpf.Services
{
    internal sealed class LanguageSettingsService : IPersistentSettingsService
    {
        public void Reload()
        {
            if (!string.IsNullOrEmpty(LanguageSettings.Default.Language))
                GlobalizationService.Current.SetCulture(LanguageSettings.Default.Language);
        }

        public void Reset()
        {
            var language = LanguageSettings.Default.Properties[nameof(LanguageSettings.Default.Language)].DefaultValue.ToString();
            if (!string.IsNullOrEmpty(language))
                GlobalizationService.Current.SetCulture(language);
        }

        public void Save()
        {
            LanguageSettings.Default.Language = CultureInfo.CurrentCulture.Name;
            LanguageSettings.Default.Save();
        }
    }
}
