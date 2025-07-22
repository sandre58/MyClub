// -----------------------------------------------------------------------
// <copyright file="MyClubDbContextFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;

namespace MyClub.Scorer.Infrastructure.Migrations.Sqlite;

public class MyClubDbContextFactory : IDesignTimeDbContextFactory<MyClubDbContext>
{
    public MyClubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyClubDbContext>();
        optionsBuilder.UseSqlite("Data Source=MyClub.db");

        return new MyClubDbContext(optionsBuilder.Options);
    }
}
