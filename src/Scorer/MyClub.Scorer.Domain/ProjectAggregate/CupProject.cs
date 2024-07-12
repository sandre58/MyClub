// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class CupProject : Project<Cup>
    {
        public CupProject(string name, byte[]? image = null, Guid? id = null)
            : base(CompetitionType.Cup, name, image, id) { }
    }
}
