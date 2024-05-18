// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.DatabaseContext.Domain;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;
using MyClub.Plugins.Teamup.Import.Database;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class ImportCompetitionsPlugin : ImportDatabaseItemsPlugin<CompetitionDto>, IImportCompetitionsPlugin
    {
        public override IEnumerable<CompetitionDto> LoadItems(IUnitOfWork unitOfWork)
            => unitOfWork.CompetitionRepository.GetAll().Select(CreateCompetitionDto);

        private static CompetitionDto CreateCompetitionDto(Competition x)
        {
            var competition = x.Type == Competition.League
                                          ? new LeagueDto()
                                          {
                                              PointsByGamesWon = x.PointsByGamesWon.GetValueOrDefault(3),
                                              PointsByGamesDrawn = x.PointsByGamesDrawn.GetValueOrDefault(1),
                                              PointsByGamesLost = x.PointsByGamesLost.GetValueOrDefault(0),
                                              SortingColumns = x.SortingColumns?.Split(";").ToList() ?? []
                                          }
                                          : new CompetitionDto();
            competition.Name = x.Name;
            competition.Category = x.Category;
            competition.ShortName = x.ShortName;
            competition.Logo = x.Logo;
            competition.MatchTime = x.MatchTime.GetValueOrDefault();
            competition.RegulationTimeDuration = x.RegulationTimeDuration;
            competition.RegulationTimeNumber = x.RegulationTimeNumber;
            competition.ExtraTimeDuration = x.ExtraTimeDuration;
            competition.ExtraTimeNumber = x.ExtraTimeNumber;
            competition.NumberOfPenaltyShootouts = x.NumberOfPenaltyShootouts;

            return competition;
        }
    }
}
