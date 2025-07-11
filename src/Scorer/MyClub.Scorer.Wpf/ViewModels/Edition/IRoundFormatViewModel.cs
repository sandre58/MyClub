// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal interface IRoundFormatViewModel : IValidatable
    {
        RoundFormatDto ProvideRoundFormat();

        void Reset();

        void Load(IRoundFormat format);
    }
}
