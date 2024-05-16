// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyNet.UI.ViewModels.Dialogs;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.Misc
{
    internal class RankingDetailsDialogViewModel : DialogViewModel
    {
        public RankingViewModel Ranking { get; }

        public RankingRules? RankingRules { get; }

        public IDictionary<TeamViewModel, int>? Penalties { get; }

        public RankingDetailsDialogViewModel(RankingViewModel ranking, RankingRules? rankingRules = null, IDictionary<TeamViewModel, int>? penalties = null)
        {
            Ranking = ranking;
            RankingRules = rankingRules;
            Penalties = penalties;
        }
    }
}
