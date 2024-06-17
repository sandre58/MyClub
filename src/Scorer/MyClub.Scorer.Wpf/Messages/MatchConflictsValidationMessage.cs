// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Messages
{
    internal class MatchConflictsValidationMessage
    {
        public MatchConflictsValidationMessage(IEnumerable<(ConflictType, MatchViewModel, MatchViewModel?)> conflicts) => Conflicts = conflicts;

        public IEnumerable<(ConflictType, MatchViewModel, MatchViewModel?)> Conflicts { get; }
    }
}
