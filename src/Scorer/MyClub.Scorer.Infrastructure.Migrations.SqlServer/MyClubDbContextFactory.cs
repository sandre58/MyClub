// -----------------------------------------------------------------------
// <copyright file="MyClubDbContextFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;

namespace MyClub.Scorer.Infrastructure.Migrations.SqlServer;

public class MyClubDbContextFactory : IDesignTimeDbContextFactory<MyClubDbContext>
{
    public MyClubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyClubDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=MyClub;Trusted_Connection=True;");

        return new MyClubDbContext(optionsBuilder.Options);
    }
}
