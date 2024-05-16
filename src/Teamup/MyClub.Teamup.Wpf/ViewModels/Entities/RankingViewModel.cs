// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyClub.Teamup.Domain.CompetitionAggregate;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class RankingViewModel : ListViewModel<RankingRowViewModel>
    {

        private readonly object _lockObject = new();

        public RankingViewModel(ReadOnlyObservableCollection<TeamViewModel> teams)
            : base(source: teams.ToObservableChangeSet().Transform(x => new RankingRowViewModel(x)), parametersProvider: new ListParametersProvider(nameof(RankingRowViewModel.Rank)))
        { }

        public int? MyRank { get; private set; }

        public RankingRules? RankingRules { get; private set; }

        public RankingRowViewModel? GetRow(TeamViewModel team) => Source.FirstOrDefault(x => x.Team == team);

        public void Update(Ranking ranking, IEnumerable<MatchViewModel> matches)
        {
            lock (_lockObject)
            {
                var matchesList = matches.ToList();
                var source = Source.ToList();

                source.ForEach(x =>
                {
                    var rowFound = ranking.FirstOrDefault(y => y.Team.Id == x.Team.Id);

                    if (rowFound is not null)
                    {
                        x.Update(rowFound, ranking.GetRank(rowFound.Team), matchesList);
                    }
                });

                MyRank = source.Find(x => x.Team.IsMyTeam)?.Rank;
                RankingRules = ranking.Rules;
            }
        }

        public override string ToString()
        {
            var str = new StringBuilder();

            Items.ForEach((x, index) => str.AppendLine($"{index + 1} : {x.ToString()}"));

            return str.ToString();
        }
    }
}
