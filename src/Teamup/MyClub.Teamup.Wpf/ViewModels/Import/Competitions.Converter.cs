// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Humanizer;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.Converters;
using MyNet.Utilities.Sequences;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionImportableConverter(CompetitionService competitionService) : IConverter<CompetitionImportDto, CompetitionImportableViewModel>
    {
        private readonly CompetitionService _competitionService = competitionService;

        public CompetitionImportableViewModel Convert(CompetitionImportDto dto)
        {
            var regulationTimeFormat = dto.RegulationTime is not null ? new HalfFormat(dto.RegulationTime.Number, dto.RegulationTime.Duration) : HalfFormat.Default;
            var extraTimeFormat = dto.ExtraTime is not null ? new HalfFormat(dto.ExtraTime.Number, dto.ExtraTime.Duration) : null;
            var matchFormat = new MatchFormat(regulationTimeFormat, extraTimeFormat, dto.NumberOfShootouts);
            return new CompetitionImportableViewModel(
                dto.Type.OrEmpty().DehumanizeTo<CompetitionType>(),
                dto.Name.OrEmpty(),
                dto.Type == CompetitionType.League.ToString()
                ? new LeagueRules(matchFormat,
                                  new RankingRules(dto.ByGamesWon.GetValueOrDefault(),
                                                   dto.ByGamesDrawn.GetValueOrDefault(),
                                                   dto.ByGamesLost.GetValueOrDefault(),
                                                   dto.RankingSortingColumns?.Select(x => x.OrEmpty().DehumanizeTo<RankingSortingColumn>(OnNoMatch.ReturnsDefault)).ToList() ?? RankingRules.DefaultSortingColumns,
                                                   dto.Labels?.ToDictionary(x => new AcceptableValueRange<int>(x.Key.min, x.Key.max), x => new RankLabel(x.Value.Color, x.Value.Name.OrEmpty(), x.Value.ShortName.OrEmpty(), x.Value.Description))),
                                  dto.MatchTime)
                : dto.Type == CompetitionType.Cup.ToString()
                ? new CupRules(matchFormat, dto.MatchTime.GetValueOrDefault())
                : dto.Type == CompetitionType.Friendly.ToString()
                ? new FriendlyRules(matchFormat, dto.MatchTime.GetValueOrDefault())
                : new CompetitionRules(matchFormat, dto.MatchTime.GetValueOrDefault()),
                _competitionService.GetSimilarCompetition(dto.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault), dto.Name.OrEmpty()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                Category = dto.Category.OrEmpty().DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault),
                Logo = dto.Logo,
                ShortName = dto.ShortName.OrEmpty()
            };
        }

        public CompetitionImportDto ConvertBack(CompetitionImportableViewModel item) => throw new NotImplementedException();
    }
}
