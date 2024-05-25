// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.DatabaseContext.Domain;
using MyClub.Plugins.Base.Database;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Plugins.Teamup.Import.Database
{
    public class ImportTeamsSourcePlugin : ImportItemsSourcePlugin<TeamImportDto>, IImportTeamsSourcePlugin
    {
        public ImportTeamsSourcePlugin() : base(DatabaseService.CreateContext) { }

        public override IEnumerable<TeamImportDto> LoadItems(IUnitOfWork unitOfWork) => DatabaseService.LoadTeams(unitOfWork);
    }
}
