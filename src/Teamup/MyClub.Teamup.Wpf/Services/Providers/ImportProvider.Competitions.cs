// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyNet.Humanizer;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class ImportCompetitionsProvider : ItemsImportablesProvider<CompetitionDto, CompetitionImportableViewModel>
    {
        private readonly CompetitionService _competitionService;
        private readonly IImportCompetitionsPlugin _importCompetitionsPlugin;

        public ImportCompetitionsProvider(IImportCompetitionsPlugin importCompetitionsPlugin, CompetitionService competitionService)
            : base(importCompetitionsPlugin.ProvideItems)
        {
            _importCompetitionsPlugin = importCompetitionsPlugin;
            _competitionService = competitionService;
        }

        public override CompetitionImportableViewModel Convert(CompetitionDto dto)
        {
            var matchFormat = new MatchFormat(new HalfFormat(dto.RegulationTimeNumber, dto.RegulationTimeDuration),
                                              dto.ExtraTimeDuration.HasValue && dto.ExtraTimeNumber.HasValue ? new HalfFormat(dto.ExtraTimeNumber.Value, dto.ExtraTimeDuration.Value) : null,
                                              dto.NumberOfPenaltyShootouts);
            return new CompetitionImportableViewModel(
                dto is LeagueDto ? CompetitionType.League : CompetitionType.Cup,
                dto.Name.OrEmpty(),
                dto is LeagueDto league
                ? new LeagueRules(matchFormat,
                                  new RankingRules(league.PointsByGamesWon,
                                                   league.PointsByGamesDrawn,
                                                   league.PointsByGamesLost,
                                                   league.SortingColumns?.Select(x => x.OrEmpty().DehumanizeTo<RankingSortingColumn>(OnNoMatch.ReturnsDefault)).ToList() ?? RankingRules.DefaultSortingColumns),
                                  dto.MatchTime)
                : new CompetitionRules(matchFormat, dto.MatchTime),
                _competitionService.GetSimilarCompetition(dto.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault), dto.Name.OrEmpty()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                Category = dto.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault),
                Logo = dto.Logo,
                ShortName = dto.ShortName.OrEmpty(),
            };
        }

        public override bool CanImport() => _importCompetitionsPlugin.CanImport();
    }
}
