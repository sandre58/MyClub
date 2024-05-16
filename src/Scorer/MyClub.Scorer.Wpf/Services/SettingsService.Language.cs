// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using MyClub.Scorer.Wpf.Settings;
using MyNet.UI.Services;
using MyNet.Utilities.Localization;

namespace MyClub.Scorer.Wpf.Services
{
    internal sealed class LanguageSettingsService : IPersistentSettingsService
    {
        public void Reload()
        {
            if (!string.IsNullOrEmpty(LanguageSettings.Default.Language))
                CultureInfoService.Current.SetCulture(LanguageSettings.Default.Language);
        }

        public void Reset()
        {
            var language = LanguageSettings.Default.Properties[nameof(LanguageSettings.Default.Language)].DefaultValue.ToString();
            if (!string.IsNullOrEmpty(language))
                CultureInfoService.Current.SetCulture(language);
        }

        public void Save()
        {
            LanguageSettings.Default.Language = CultureInfo.CurrentCulture.Name;
            LanguageSettings.Default.Save();
        }
    }
}
