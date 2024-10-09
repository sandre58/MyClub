// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class CupProject : Project<Cup>
    {
        public CupProject(string name, byte[]? image = null, Guid? id = null)
            : base(CompetitionType.Cup, name, image, id) { }

        public override Team AddTeam(Team team)
        {
            base.AddTeam(team);
            return (Team)Competition.AddTeam(team);
        }

        public override bool RemoveTeam(Team team, bool removeStadium = false)
        {
            base.RemoveTeam(team, removeStadium);
            return Competition.RemoveTeam(team);
        }
    }
}
