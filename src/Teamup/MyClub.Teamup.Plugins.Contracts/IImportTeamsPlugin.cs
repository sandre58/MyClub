// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Teamup.Plugins.Contracts
{
    public interface IImportTeamsPlugin
    {
        IEnumerable<TeamDto> ProvideItems();

        bool CanImport();
    }
}
