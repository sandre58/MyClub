// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Import;

namespace MyClub.Teamup.Wpf.Services.Providers
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
                var isSimilar = _teamService.GetSimilarTeams(x.Name.OrEmpty(), x.Category.OrThrow()).Any();
                var team = new TeamImportableViewModel(x.Club.OrEmpty(), isSimilar ? ImportMode.Update : ImportMode.Add)
                {
                    ShortName = x.ShortName.OrEmpty(),
                    Category = x.Category,
                    HomeColor = x.HomeColor?.ToColor(),
                    AwayColor = x.AwayColor?.ToColor(),
                    Name = x.Name.OrEmpty(),
                    Logo = x.Logo,
                    Country = x.Country,
                    Stadium = x.Stadium is not null
                              ? new StadiumImportableViewModel(x.Name.OrEmpty())
                              {
                                  Address = x.Stadium.Address?.Street,
                                  City = x.Stadium.Address?.City,
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
