// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using MyClub.DatabaseContext.Domain.ClubAggregate;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;
using MyClub.DatabaseContext.Domain.PlayerAggregate;
using MyClub.DatabaseContext.Domain.StadiumAggregate;
using MyClub.DatabaseContext.Infrastructure.Data.Configuration;

namespace MyClub.DatabaseContext.Infrastructure.Data
{
    public class MyTeamup(DbContextOptions<MyTeamup> options) : DbContext(options)
    {
        public DbSet<Competition> Competitions { get; set; }

        public DbSet<Team> Teams { get; set; }

        public DbSet<Club> Clubs { get; set; }

        public DbSet<Stadium> Stadiums { get; set; }

        public DbSet<Player> Players { get; set; }

        public DbSet<Injury> Injuries { get; set; }

        public DbSet<Email> Emails { get; set; }

        public DbSet<Phone> Phones { get; set; }

        public DbSet<RatedPosition> Positions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new CompetitionEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new StadiumEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ClubEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new TeamEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new PlayerEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new InjuryEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new EmailEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new PhoneEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new RatedPositionEntityTypeConfiguration());
        }
    }
}
