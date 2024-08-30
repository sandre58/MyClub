// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Plugins.Contracts.Dtos;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumsImportSourceViewModel : ImportSourceViewModel<StadiumImportDto, StadiumImportableViewModel>
    {
        public StadiumsImportSourceViewModel(IImportStadiumsSourcePlugin source, Func<string, string?, bool> isSimilar)
            : base(source, new StadiumImportableConverter(isSimilar)) { }
    }
}
