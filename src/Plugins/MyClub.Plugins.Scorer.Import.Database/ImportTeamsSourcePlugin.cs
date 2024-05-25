// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.DatabaseContext.Domain;
using MyClub.Plugins.Base.Database;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Plugins.Contracts.Dtos;

namespace MyClub.Plugins.Scorer.Import.Database
{
    public class ImportTeamsSourcePlugin : ImportItemsSourcePlugin<TeamImportDto>, IImportTeamsSourcePlugin
    {
        public ImportTeamsSourcePlugin() : base(DatabaseService.CreateContext) { }

        public override IEnumerable<TeamImportDto> LoadItems(IUnitOfWork unitOfWork) => DatabaseService.LoadTeams(unitOfWork);
    }
}
