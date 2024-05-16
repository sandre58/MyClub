// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.DatabaseContext.Domain.ClubAggregate;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;
using MyClub.DatabaseContext.Domain.PlayerAggregate;
using MyClub.DatabaseContext.Domain.StadiumAggregate;

namespace MyClub.DatabaseContext.Domain
{
    public interface IUnitOfWork : IDisposable
    {
        IStadiumRepository StadiumRepository { get; }

        ITeamRepository TeamRepository { get; }

        IClubRepository ClubRepository { get; }

        IPlayerRepository PlayerRepository { get; }

        ICompetitionRepository CompetitionRepository { get; }

        (string name, string host) GetConnectionInfo();

        bool CanConnect();

        int Save();
    }
}
