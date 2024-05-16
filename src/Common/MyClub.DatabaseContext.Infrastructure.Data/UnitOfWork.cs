// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.EntityFrameworkCore;
using MyClub.DatabaseContext.Domain;
using MyClub.DatabaseContext.Domain.ClubAggregate;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;
using MyClub.DatabaseContext.Domain.PlayerAggregate;
using MyClub.DatabaseContext.Domain.StadiumAggregate;
using MyClub.DatabaseContext.Infrastructure.Data.Repositories;

namespace MyClub.DatabaseContext.Infrastructure.Data
{
    public class UnitOfWork(MyTeamup context) : IUnitOfWork
    {
        private readonly MyTeamup _context = context;
        private bool _disposed = false;

        public IStadiumRepository StadiumRepository { get; private set; } = new StadiumRepository(context);

        public ITeamRepository TeamRepository { get; private set; } = new TeamRepository(context);

        public IClubRepository ClubRepository { get; private set; } = new ClubRepository(context);

        public IPlayerRepository PlayerRepository { get; private set; } = new PlayerRepository(context);

        public ICompetitionRepository CompetitionRepository { get; private set; } = new CompetitionRepository(context);

        public (string name, string host) GetConnectionInfo()
        {
            var connection = _context.Database.GetDbConnection();

            return (connection.Database, connection.DataSource);
        }

        public bool CanConnect() => _context.Database.CanConnect();

        public int Save() => _context.SaveChanges();

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
