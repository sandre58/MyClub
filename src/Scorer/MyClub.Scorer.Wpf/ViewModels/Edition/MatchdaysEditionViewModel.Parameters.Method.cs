// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal interface IAddMatchdaysMethodViewModel : IValidatable
    {
        AddMatchdaysDatesParametersDto ProvideDatesParameters();

        void Reset(LeagueViewModel stage);
    }
}
