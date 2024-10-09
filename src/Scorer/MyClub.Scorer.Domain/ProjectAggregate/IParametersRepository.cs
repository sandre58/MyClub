// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public interface IParametersRepository
    {
        MatchFormat GetMatchFormat();

        MatchRules GetMatchRules();

        SchedulingParameters GetSchedulingParameters();

        ProjectPreferences GetPreferences();

        void UpdateMatchFormat(MatchFormat format);

        void UpdateMatchRules(MatchRules rules);

        void UpdateSchedulingParameters(SchedulingParameters schedulingParameters);

        void UpdatePreferences(ProjectPreferences projectPreferences);
    }
}
