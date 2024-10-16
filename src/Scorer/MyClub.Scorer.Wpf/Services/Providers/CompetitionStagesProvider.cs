// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class CompetitionStagesProvider : EntitiesProviderBase<ICompetitionStageViewModel>
    {
        private readonly CompetitionInfoProvider _competitionInfoProvider;

        public CompetitionStagesProvider(ProjectInfoProvider projectInfoProvider, CompetitionInfoProvider competitionInfoProvider) : base(projectInfoProvider) => _competitionInfoProvider = competitionInfoProvider;

        protected override IObservable<IChangeSet<ICompetitionStageViewModel>> ProvideObservable(IProject project)
            => _competitionInfoProvider.GetCompetition() switch
            {
                LeagueViewModel league => league.Matchdays.ToObservableChangeSet().Transform(x => x.CastIn<ICompetitionStageViewModel>()),
                CupViewModel cup => cup.Rounds.ToObservableChangeSet().Transform(x => x.CastIn<ICompetitionStageViewModel>()),
                _ => throw new NotImplementedException(),
            };

        public IStageViewModel? Previous(DateTime date)
            => Items?.OrderBy(x => x.StartDate).LastOrDefault(x => x.StartDate.IsBefore(date));

        public IStageViewModel? Previous(ICompetitionStageViewModel stage)
            => Items?.OrderBy(x => x.StartDate).LastOrDefault(x => x.StartDate.IsBefore(stage.StartDate));

        public IStageViewModel? Next(DateTime date)
            => Items?.OrderBy(x => x.StartDate).FirstOrDefault(x => x.StartDate.IsAfter(date));

        public IStageViewModel? Next(ICompetitionStageViewModel stage)
            => Items?.OrderBy(x => x.StartDate).FirstOrDefault(x => x.StartDate.IsAfter(stage.StartDate));
    }
}
