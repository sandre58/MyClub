// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.UI.Selection;
using MyNet.UI.ViewModels.Selection;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumsDatabaseImportViewModel : ItemsSelectionViewModel<StadiumImportableViewModel>
    {
        public StadiumsDatabaseImportViewModel(DatabaseService databaseService,
                                      StadiumService stadiumService,
                                      Func<StadiumImportableViewModel, bool>? predicate = null,
                                      SelectionMode selectionMode = SelectionMode.Single)
            : base(new StadiumsDatabaseProvider(databaseService, stadiumService, predicate),
                  parametersProvider: new StadiumsImportListParametersProvider(),
                  selectionMode: selectionMode)
        { }
    }
}
