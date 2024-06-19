// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class LeagueProject : Project<League>
    {
        public LeagueProject(string name, byte[]? image = null, SchedulingParameters? schedulingParameters = null, Guid? id = null)
            : base(CompetitionType.League, name, image, id) => SchedulingParameters = schedulingParameters ?? SchedulingParameters.Default;

        public SchedulingParameters SchedulingParameters { get; set; }

        public override Team AddTeam(Team team)
        {
            base.AddTeam(team);
            return Competition.AddTeam(team);
        }

        public override bool RemoveTeam(Team team, bool removeStadium = false)
        {
            base.RemoveTeam(team, removeStadium);
            return Competition.RemoveTeam(team);
        }
    }
}
