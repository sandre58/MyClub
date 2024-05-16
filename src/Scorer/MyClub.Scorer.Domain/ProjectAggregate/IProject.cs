// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public interface IProject : IAuditableEntity, IAggregateRoot
    {
        ICompetition Competition { get; }

        ProjectParameters Parameters { get; }

        CompetitionType Type { get; }

        string Name { get; set; }

        byte[]? Image { get; set; }

        DateTime StartDate { get; }

        DateTime EndDate { get; }

        ReadOnlyObservableCollection<Team> Teams { get; }

        ReadOnlyObservableCollection<Stadium> Stadiums { get; }

        Team AddTeam(Team team);

        bool RemoveTeam(Team team, bool removeStadium = false);

        Stadium AddStadium(Stadium stadium);

        bool RemoveStadium(Stadium stadium);
    }
}
