// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Settings;
using MyNet.UI.Services;

namespace MyClub.Teamup.Wpf.Services
{
    internal class AppSettingsService : IPersistentSettingsService
    {
        public void Reload() => AppSettings.Default?.Reload();

        public void Reset() => AppSettings.Default.Reset();

        public void Save() => AppSettings.Default.Save();
    }
}
