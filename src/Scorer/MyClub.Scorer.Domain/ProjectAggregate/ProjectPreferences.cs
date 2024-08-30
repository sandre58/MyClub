// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class ProjectPreferences : Entity
    {
        public bool TreatNoStadiumAsWarning { get; set; } = true;
    }
}
