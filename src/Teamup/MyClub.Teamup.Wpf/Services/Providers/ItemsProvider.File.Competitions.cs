// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Humanizer;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyClub.Teamup.Application.Dtos;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class CompetitionsFileProvider : ItemsFileProvider<CompetitionImportableViewModel>
    {
        private readonly CompetitionsImportService _competitionsImportService;
        private readonly CompetitionService _competitionService;

        public CompetitionsFileProvider(CompetitionsImportService competitionsImportService, CompetitionService competitionService, Func<CompetitionImportableViewModel, bool>? predicate = null) : base(predicate: predicate)
        {
            _competitionService = competitionService;
            _competitionsImportService = competitionsImportService;
        }

        protected override (IEnumerable<CompetitionImportableViewModel> items, IEnumerable<Exception> exceptions) LoadItems(string filename)
        {
            ICollection<CompetitionImportableViewModel> importableCompetitions;

            var (competitions, exceptions) = _competitionsImportService.ExtractCompetitions(filename);

            importableCompetitions = competitions.Select(x =>
            {
                var isSimilar = _competitionService.GetSimilarCompetition(x.Category, x.Name.OrEmpty()).Any();
                var type = x.Type.OrEmpty().DehumanizeTo<CompetitionType>(OnNoMatch.ReturnsDefault);
                var competition = new CompetitionImportableViewModel(type,
                                                                     x.Name.OrEmpty(),
                                                                     type == CompetitionType.League
                                                                     ? new LeagueRules(new MatchFormat(x.RegulationTime ?? HalfFormat.Default, x.ExtraTime, x.NumberOfShootouts), new RankingRules(x.ByGamesWon ?? 3, x.ByGamesDrawn ?? 1, x.ByGamesLost ?? 0, x.RankingSortingColumns?.ToList() ?? RankingRules.DefaultSortingColumns, x.Labels), x.MatchTime)
                                                                     : new CompetitionRules(new MatchFormat(x.RegulationTime ?? HalfFormat.Default, x.ExtraTime, x.NumberOfShootouts), x.MatchTime ?? 15.Hours()),
                                                                     isSimilar ? ImportMode.Update : ImportMode.Add)
                {
                    Category = x.Category,
                    Logo = x.Logo,
                    ShortName = x.ShortName.OrEmpty()
                };

                competition.ValidateProperties();
                return competition;
            }).ToList();

            return new(importableCompetitions, exceptions);
        }
    }
}
