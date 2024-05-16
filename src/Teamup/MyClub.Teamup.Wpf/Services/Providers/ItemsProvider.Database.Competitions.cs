// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.DatabaseContext.Application.Dtos;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Import;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class CompetitionsDatabaseProvider : ItemsDatabaseProvider<CompetitionImportableViewModel>
    {
        private readonly CompetitionService _competitionService;

        public CompetitionsDatabaseProvider(DatabaseService databaseService, CompetitionService competitionService, Func<CompetitionImportableViewModel, bool>? predicate = null) : base(databaseService, predicate) => _competitionService = competitionService;

        public override IEnumerable<CompetitionImportableViewModel> LoadItems()
            => DatabaseService.GetCompetitions().Select(x =>
            {
                var matchFormat = new MatchFormat(new HalfFormat(x.RegulationTimeNumber, x.RegulationTimeDuration),
                                                  x.ExtraTimeDuration.HasValue && x.ExtraTimeNumber.HasValue ? new HalfFormat(x.ExtraTimeNumber.Value, x.ExtraTimeDuration.Value) : null,
                                                  x.NumberOfPenaltyShootouts);
                return new CompetitionImportableViewModel(
                    x is LeagueDto ? CompetitionType.League : CompetitionType.Cup,
                    x.Name.OrEmpty(),
                    x is LeagueDto league
                    ? new LeagueRules(matchFormat,
                                      new RankingRules(league.PointsByGamesWon,
                                                       league.PointsByGamesDrawn,
                                                       league.PointsByGamesLost,
                                                       league.SortingColumns ?? RankingRules.DefaultSortingColumns),
                                      x.MatchTime)
                    : new CompetitionRules(matchFormat, x.MatchTime),
                    _competitionService.GetSimilarCompetition(x.Category, x.Name.OrEmpty()).Any() ? ImportMode.Update : ImportMode.Add)
                {
                    Category = x.Category,
                    Logo = x.Logo,
                    ShortName = x.ShortName.OrEmpty(),
                };
            });
    }
}
