// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class CompetitionsProvider : EntitiesProviderBase<ICompetitionSeason, CompetitionViewModel>
    {
        public CompetitionsProvider(ProjectInfoProvider projectInfoProvider,
                                    TeamsProvider teamsProvider,
                                    StadiumsProvider stadiumsProvider,
                                    FriendlyPresentationService friendlyPresentationService,
                                    LeaguePresentationService leaguePresentationService,
                                    CupPresentationService cupPresentationService,
                                    RoundPresentationService roundPresentationService,
                                    MatchdayPresentationService matchdayPresentationService,
                                    MatchPresentationService matchPresentationService)
            : base(projectInfoProvider, x => x switch
                {
                    FriendlySeason friendly => new FriendlyViewModel(friendly, friendlyPresentationService, matchPresentationService, teamsProvider, stadiumsProvider),
                    LeagueSeason league => new LeagueViewModel(league, leaguePresentationService, matchdayPresentationService, matchPresentationService, teamsProvider, stadiumsProvider),
                    CupSeason cup => new CupViewModel(cup, cupPresentationService, roundPresentationService, matchdayPresentationService, matchPresentationService, teamsProvider, stadiumsProvider),
                    _ => throw new InvalidOperationException("Competition type is unknown"),
                })
        { }

        protected override IObservable<IChangeSet<ICompetitionSeason, Guid>> ProvideObservable(Project project) => project.Competitions.ToObservableChangeSet(x => x.Id);
    }
}
