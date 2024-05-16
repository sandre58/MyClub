// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Services;

namespace MyClub.Scorer.Wpf.Services
{
    internal class SettingsService(
        LanguageSettingsService languageSettingsService,
        ThemeSettingsService themeSettingsService,
        AppSettingsService appSettingsService) : PreferencesService(new IPersistentSettingsService[] { languageSettingsService, themeSettingsService, appSettingsService })
    {
    }
}
