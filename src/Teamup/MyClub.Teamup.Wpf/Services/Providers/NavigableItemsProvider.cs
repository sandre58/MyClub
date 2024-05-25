// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.Utilities;
using MyNet.Observable.Collections.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class NavigableItemsProvider : ObservableSourceProvider<ISearchableItem>
    {
        public NavigableItemsProvider(PlayersProvider playersProvider,
                                      CompetitionsProvider competitionsProvider,
                                      TrainingSessionsProvider trainingSessionsProvider,
                                      TeamsProvider teamsProvider,
                                      StadiumsProvider stadiumsProvider,
                                      ActionsProvider actionsProvider)
            : base(playersProvider.Connect().Sort(SortExpressionComparer<PlayerViewModel>.Ascending(x => x.InverseName)).Transform(x => (ISearchableItem)x)
                                  .Merge(trainingSessionsProvider.Connect().Sort(SortExpressionComparer<TrainingSessionViewModel>.Ascending(x => x.StartDate)).Transform(x => (ISearchableItem)x))
                                  .Merge(competitionsProvider.Connect().Sort(SortExpressionComparer<CompetitionViewModel>.Ascending(x => x.Name)).Transform(x => (ISearchableItem)x))
                                  .Merge(teamsProvider.Connect().Sort(SortExpressionComparer<TeamViewModel>.Ascending(x => x.Name)).Transform(x => (ISearchableItem)x))
                                  .Merge(stadiumsProvider.Connect().Sort(SortExpressionComparer<StadiumViewModel>.Ascending(x => x.DisplayName)).Transform(x => (ISearchableItem)x))
                                  .Merge(actionsProvider.Connect().Transform(x => (ISearchableItem)x))
                  )
        { }
    }
}
