// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class ProjectPreferences : Entity
    {
        public bool TreatNoStadiumAsWarning { get; set; } = true;

        public TimeSpan PeriodForPreviousMatches { get; set; } = 8.Days();

        public TimeSpan PeriodForNextMatches { get; set; } = 8.Days();

        public bool ShowNextMatchFallback { get; set; } = true;

        public bool ShowLastMatchFallback { get; set; } = true;
    }
}
