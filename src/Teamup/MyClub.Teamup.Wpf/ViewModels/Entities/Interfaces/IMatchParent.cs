// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using MyNet.Utilities;
using MyClub.Teamup.Domain.CompetitionAggregate;

namespace MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IMatchParent : IIdentifiable<Guid>
    {
        string Name { get; }

        string ShortName { get; }

        Color Color { get; }

        IMatchParent? Parent { get; }

        ReadOnlyObservableCollection<MatchViewModel> AllMatches { get; }

        CompetitionRules Rules { get; }

        IEnumerable<TeamViewModel> GetAvailableTeams();

        DateTime GetDefaultDateTime();

        bool CanCancelMatch();

        bool CanEditMatchFormat();

        bool CanEditPenaltyPoints();
    }
}
