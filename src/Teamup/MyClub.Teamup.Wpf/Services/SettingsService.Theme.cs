// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Settings;
using MyNet.Humanizer;
using MyNet.UI.Services;
using MyNet.UI.Theming;

namespace MyClub.Teamup.Wpf.Services
{
    internal sealed class ThemeSettingsService : IPersistentSettingsService
    {
        public void Reload()
            => ThemeManager.ApplyTheme(new Theme
            {
                Base = ThemeSettings.Default.Base.DehumanizeTo<ThemeBase>(),
                PrimaryColor = string.IsNullOrEmpty(ThemeSettings.Default.PrimaryColor) ? null : ThemeSettings.Default.PrimaryColor,
                AccentColor = string.IsNullOrEmpty(ThemeSettings.Default.AccentColor) ? null : ThemeSettings.Default.AccentColor,
                PrimaryForegroundColor = string.IsNullOrEmpty(ThemeSettings.Default.PrimaryForegroundColor) ? null : ThemeSettings.Default.PrimaryForegroundColor,
                AccentForegroundColor = string.IsNullOrEmpty(ThemeSettings.Default.AccentForegroundColor) ? null : ThemeSettings.Default.AccentForegroundColor
            });

        public void Reset()
        {
            var defaultPrimaryColor = ThemeSettings.Default.Properties[nameof(ThemeSettings.Default.PrimaryColor)].DefaultValue.ToString();
            var defaultAccentColor = ThemeSettings.Default.Properties[nameof(ThemeSettings.Default.AccentColor)].DefaultValue.ToString();
            var defaultPrimaryForegroundColor = ThemeSettings.Default.Properties[nameof(ThemeSettings.Default.PrimaryForegroundColor)].DefaultValue.ToString();
            var defaultAccentForegroundColor = ThemeSettings.Default.Properties[nameof(ThemeSettings.Default.AccentForegroundColor)].DefaultValue.ToString();
            ThemeManager.ApplyTheme(new Theme
            {
                Base = ThemeSettings.Default.Properties[nameof(ThemeSettings.Default.Base)].DefaultValue.ToString()?.DehumanizeTo<ThemeBase>(),
                PrimaryColor = string.IsNullOrEmpty(defaultPrimaryColor) ? null : defaultPrimaryColor,
                AccentColor = string.IsNullOrEmpty(defaultAccentColor) ? null : defaultAccentColor,
                PrimaryForegroundColor = string.IsNullOrEmpty(defaultPrimaryForegroundColor) ? null : defaultPrimaryForegroundColor,
                AccentForegroundColor = string.IsNullOrEmpty(defaultAccentForegroundColor) ? null : defaultAccentForegroundColor
            });
        }

        public void Save()
        {
            if (ThemeManager.CurrentTheme is null) return;

            ThemeSettings.Default.Base = ThemeManager.CurrentTheme.Base?.ToString();
            ThemeSettings.Default.AccentColor = ThemeManager.CurrentTheme.AccentColor;
            ThemeSettings.Default.PrimaryColor = ThemeManager.CurrentTheme.PrimaryColor;
            ThemeSettings.Default.AccentForegroundColor = ThemeManager.CurrentTheme.AccentForegroundColor;
            ThemeSettings.Default.PrimaryForegroundColor = ThemeManager.CurrentTheme.PrimaryForegroundColor;

            ThemeSettings.Default.Save();
        }
    }
}
