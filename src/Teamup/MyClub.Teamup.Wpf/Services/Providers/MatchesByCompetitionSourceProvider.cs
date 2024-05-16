// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class MatchesByCompetitionSourceProvider<TCompetition> : ItemChangedSourceProvider<MatchViewModel, TCompetition>
        where TCompetition : CompetitionViewModel
    {
        public MatchesByCompetitionSourceProvider(Subject<TCompetition?> competitionChanged) : base(competitionChanged, x => x.AllMatches.ToObservableChangeSet(x => x.Id)) { }
    }
}
