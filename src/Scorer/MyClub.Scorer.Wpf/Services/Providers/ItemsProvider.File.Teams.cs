// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.IO;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class TeamsFileProvider : ItemsFileProvider<TeamImportableViewModel>
    {
        private readonly TeamsImportService _teamsImportService;
        private readonly TeamService _teamService;

        public TeamsFileProvider(TeamsImportService teamsImportService, TeamService teamService, Func<TeamImportableViewModel, bool>? predicate = null) : base(predicate: predicate)
        {
            _teamService = teamService;
            _teamsImportService = teamsImportService;
        }

        protected override (IEnumerable<TeamImportableViewModel> items, IEnumerable<Exception> exceptions) LoadItems(string filename)
        {
            ICollection<TeamImportableViewModel> importableTeams;

            var (teams, exceptions) = _teamsImportService.ExtractTeams(filename);

            importableTeams = teams.Select(x =>
            {
                var isSimilar = _teamService.GetSimilarTeams(x.Name.OrEmpty()).Any();
                var team = new TeamImportableViewModel(x.Name.OrEmpty(), mode: isSimilar ? ImportMode.Update : ImportMode.Add)
                {
                    ShortName = x.ShortName.OrEmpty(),
                    HomeColor = x.HomeColor?.ToColor(),
                    AwayColor = x.AwayColor?.ToColor(),
                    Logo = x.Logo,
                    Country = x.Country,
                    Stadium = x.Stadium is not null
                              ? new StadiumImportableViewModel(x.Stadium.Name.OrEmpty(), x.Stadium.Address?.City)
                              {
                                  Address = x.Stadium.Address?.Street,
                                  Country = x.Stadium.Address?.Country,
                                  Ground = x.Stadium.Ground,
                                  Latitude = x.Stadium.Address?.Latitude,
                                  Longitude = x.Stadium.Address?.Longitude,
                                  PostalCode = x.Stadium.Address?.PostalCode
                              } : null
                };

                team.ValidateProperties();
                return team;
            }).ToList();

            return new(importableTeams, exceptions);
        }
    }
}
