// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionsImportSourceViewModel : ImportSourceViewModel<CompetitionImportDto, CompetitionImportableViewModel>
    {
        public CompetitionsImportSourceViewModel(IImportCompetitionsSourcePlugin source, CompetitionService competitionService)
            : base(source, new CompetitionImportableConverter(competitionService)) { }
    }
}
