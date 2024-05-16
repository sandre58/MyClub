// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyClub.Teamup.Application.Services;
using MyClub.DatabaseContext.Application.Dtos;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Import;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class TeamsDatabaseProvider : ItemsDatabaseProvider<TeamImportableViewModel>
    {
        private readonly TeamService _teamService;

        public TeamsDatabaseProvider(DatabaseService databaseService, TeamService teamService, Func<TeamImportableViewModel, bool>? predicate = null) : base(databaseService, predicate) => _teamService = teamService;

        public override IEnumerable<TeamImportableViewModel> LoadItems()
            => DatabaseService.GetTeams().Select(x => new TeamImportableViewModel(x.Club.OrThrow().Name.OrEmpty(), _teamService.GetSimilarTeams(x.Name.OrEmpty(), x.Category.OrThrow()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                AwayColor = x.AwayColor?.ToColor() ?? x.Club.OrThrow().AwayColor?.ToColor(),
                HomeColor = x.HomeColor?.ToColor() ?? x.Club.OrThrow().HomeColor?.ToColor(),
                Country = x.Club.OrThrow().Country,
                Logo = x.Club.OrThrow().Logo,
                ShortName = x.ShortName.OrEmpty(),
                Name = x.Name.OrEmpty(),
                Category = x.Category,
                Stadium = (x.Stadium ?? x.Club.OrThrow().Stadium) is StadiumDto stadium
                          ? new StadiumImportableViewModel(stadium.Name.OrEmpty())
                          {
                              Ground = stadium.Ground,
                              Address = stadium.Address?.Street,
                              City = stadium.Address?.City,
                              Country = stadium.Address?.Country,
                              Latitude = stadium.Address?.Latitude,
                              Longitude = stadium.Address?.Longitude,
                              PostalCode = stadium.Address?.PostalCode
                          } : null
            });
    }
}
