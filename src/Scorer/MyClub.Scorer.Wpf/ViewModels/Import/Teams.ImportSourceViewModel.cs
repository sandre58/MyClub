// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Plugins.Contracts.Dtos;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class TeamsImportSourceViewModel : ImportSourceViewModel<TeamImportDto, TeamImportableViewModel>
    {
        public TeamsImportSourceViewModel(IImportTeamsSourcePlugin source, Func<string, bool> isSimilar)
            : base(source, new TeamImportableConverter(isSimilar)) { }
    }
}
