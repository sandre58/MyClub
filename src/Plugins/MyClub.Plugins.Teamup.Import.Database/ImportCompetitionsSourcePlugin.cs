// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.DatabaseContext.Domain;
using MyClub.Plugins.Base.Database;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Plugins.Teamup.Import.Database
{
    public class ImportCompetitionsSourcePlugin : ImportItemsSourcePlugin<CompetitionImportDto>, IImportCompetitionsSourcePlugin
    {
        public ImportCompetitionsSourcePlugin() : base(DatabaseService.CreateContext) { }

        public override IEnumerable<CompetitionImportDto> LoadItems(IUnitOfWork unitOfWork) => DatabaseService.LoadCompetitions(unitOfWork);
    }
}
