// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Dialogs;
using MyNet.UI.Locators;
using MyNet.UI.Extensions;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services
{
    internal class LeaguePresentationService(CompetitionService service, IViewModelLocator viewModelLocator) : PresentationServiceBase<LeagueViewModel, LeagueEditionViewModel, CompetitionService>(service, viewModelLocator)
    {
        public async Task<Guid?> DuplicateAsync(LeagueViewModel item)
            => await AddAsync(x =>
            {
                var itemToDuplicated = Service.GetById(item.Id);

                if (itemToDuplicated is not null)
                {
                    x.Name = itemToDuplicated.Competition.Name.Increment(Service.GetAll().Select(x => x.Competition.Name), format: " (#)");
                    x.ShortName = itemToDuplicated.Competition.ShortName;
                    x.StartDate = itemToDuplicated.Period.Start;
                    x.EndDate = itemToDuplicated.Period.End;
                    x.MatchTime = itemToDuplicated.Rules.MatchTime;
                    x.MatchFormat.Load(itemToDuplicated.Rules.MatchFormat);
                    x.PointsByGamesWon = ((LeagueRules)itemToDuplicated.Rules).RankingRules.PointsByGamesWon;
                    x.PointsByGamesDrawn = ((LeagueRules)itemToDuplicated.Rules).RankingRules.PointsByGamesDrawn;
                    x.PointsByGamesLost = ((LeagueRules)itemToDuplicated.Rules).RankingRules.PointsByGamesLost;
                    x.SortingColumns.Set(((LeagueRules)itemToDuplicated.Rules).RankingRules.SortingColumns);
                    x.RankingLabels.Set(((LeagueRules)itemToDuplicated.Rules).RankingRules.Labels.Select(y => new EditableRankLabelViewModel
                    {
                        Color = y.Value.Color?.ToColor(),
                        Description = y.Value.Description,
                        FromRank = y.Key.Min ?? 1,
                        Name = y.Value.Name,
                        ShortName = y.Value.ShortName,
                        ToRank = y.Key.Max ?? 1,
                    }));
                }
            }).ConfigureAwait(false);

        public async Task<EditableRankLabelViewModel?> CreateRankLabelAsync()
        {
            var vm = ViewModelLocator.Get<RankLabelEditionViewModel>();
            vm.New();

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                var item = new EditableRankLabelViewModel
                {
                    FromRank = vm.FromRank,
                    ToRank = vm.ToRank,
                    Color = vm.Color,
                    Description = vm.Description,
                    Name = vm.Name,
                    ShortName = vm.ShortName
                };

                return item;
            }

            return null;
        }

        public async Task<bool?> UpdateRankLabelAsync(EditableRankLabelViewModel oldItem)
        {
            var vm = ViewModelLocator.Get<RankLabelEditionViewModel>();
            vm.Load(oldItem);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                oldItem.Name = vm.Name;
                oldItem.ShortName = vm.ShortName;
                oldItem.FromRank = vm.FromRank;
                oldItem.ToRank = vm.ToRank;
                oldItem.Color = vm.Color;
                oldItem.Description = vm.Description;
            }

            return result;
        }

        public int Remove(IEnumerable<LeagueViewModel> oldItems) => !oldItems.Any() ? 0 : Service.Remove(oldItems.Select(x => x.Id).ToList());
    }
}
