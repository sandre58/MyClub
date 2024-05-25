// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Teamup.Wpf.Services.Providers.Fakes
{
    internal class FakeImportTeamsPlugin : IImportTeamsPlugin
    {
        public bool IsEnabled() => false;

        public IEnumerable<TeamImportDto> ProvideItems() => [];
    }
}
