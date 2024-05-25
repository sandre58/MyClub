// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Plugins.Contracts.Dtos;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumsImportSourceViewModel : ImportSourceViewModel<StadiumImportDto, StadiumImportableViewModel>
    {
        public StadiumsImportSourceViewModel(IImportStadiumsSourcePlugin source, StadiumService stadiumService)
            : base(source, new StadiumImportableConverter(stadiumService)) { }
    }
}
