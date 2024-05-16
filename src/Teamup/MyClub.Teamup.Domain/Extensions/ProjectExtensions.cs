// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.Extensions
{
    public static class ProjectExtensions
    {
        public static List<Team> GetMainTeams(this Project project) => project.MainTeam is not null ? [project.MainTeam] : [.. project.Club.Teams];
    }
}
