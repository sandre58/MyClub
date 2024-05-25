// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Plugins.Contracts.Base;
using MyClub.Scorer.Plugins.Contracts.Dtos;

namespace MyClub.Scorer.Plugins.Contracts
{
    public interface IImportTeamsSourcePlugin : IImportSourcePlugin<TeamImportDto>
    {
    }
}
