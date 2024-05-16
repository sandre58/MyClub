// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.ViewModels.Import;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyClub.DatabaseContext.Application.Dtos;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyClub.Scorer.Application.Services;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class TeamsDatabaseProvider : ItemsDatabaseProvider<TeamImportableViewModel>
    {
        private readonly TeamService _teamService;

        public TeamsDatabaseProvider(DatabaseService databaseService, TeamService teamService, Func<TeamImportableViewModel, bool>? predicate = null) : base(databaseService, predicate) => _teamService = teamService;

        public override IEnumerable<TeamImportableViewModel> LoadItems()
            => DatabaseService.GetTeams().Select(x => new TeamImportableViewModel(x.Name.OrEmpty(), mode: _teamService.GetSimilarTeams(x.Name.OrEmpty()).Any() ? ImportMode.Update : ImportMode.Add)
            {
                AwayColor = x.AwayColor?.ToColor() ?? x.Club.OrThrow().AwayColor?.ToColor(),
                HomeColor = x.HomeColor?.ToColor() ?? x.Club.OrThrow().HomeColor?.ToColor(),
                Country = x.Club.OrThrow().Country,
                Logo = x.Club.OrThrow().Logo,
                ShortName = x.ShortName.OrEmpty(),
                Stadium = (x.Stadium ?? x.Club.OrThrow().Stadium) is StadiumDto stadium
                          ? new StadiumImportableViewModel(stadium.Name.OrEmpty(), stadium.Address?.City)
                          {
                              Ground = stadium.Ground,
                              Address = stadium.Address?.Street,
                              Country = stadium.Address?.Country,
                              Latitude = stadium.Address?.Latitude,
                              Longitude = stadium.Address?.Longitude,
                              PostalCode = stadium.Address?.PostalCode
                          } : null
            });
    }
}
