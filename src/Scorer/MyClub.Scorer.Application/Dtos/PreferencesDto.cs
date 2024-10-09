// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Application.Dtos
{
    public class PreferencesDto
    {
        public bool TreatNoStadiumAsWarning { get; set; }

        public TimeSpan PeriodForPreviousMatches { get; set; }

        public TimeSpan PeriodForNextMatches { get; set; }

        public bool ShowLastMatchFallback { get; set; }

        public bool ShowNextMatchFallback { get; set; }
    }
}
