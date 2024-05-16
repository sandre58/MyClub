// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.Enums;

namespace MyClub.Scorer.Application.Messages
{
    public class MatchConflictsValidationMessage
    {
        public MatchConflictsValidationMessage(IEnumerable<(ConflictType, Guid, Guid?)> conflicts) => Conflicts = conflicts;

        public IEnumerable<(ConflictType, Guid, Guid?)> Conflicts { get; }
    }
}
