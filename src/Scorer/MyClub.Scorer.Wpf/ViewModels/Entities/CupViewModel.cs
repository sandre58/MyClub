// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class CupViewModel : EntityViewModelBase<Cup>, ICompetitionViewModel
    {
        public CupViewModel(Cup item) : base(item) { }

        public IObservable<IChangeSet<MatchViewModel, Guid>> ProvideMatches() => throw new NotImplementedException();

        public IObservable<IChangeSet<IMatchParent, Guid>> ProvideMatchParents() => throw new NotImplementedException();
    }
}
