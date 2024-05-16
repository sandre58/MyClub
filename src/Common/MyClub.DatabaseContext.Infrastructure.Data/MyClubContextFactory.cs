// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MyClub.DatabaseContext.Infrastructure.Data
{
    public class MyTeamupFactory : IDesignTimeDbContextFactory<MyTeamup>
    {
        public MyTeamup CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("Default");
            var optionsBuilder = new DbContextOptionsBuilder<MyTeamup>();
            optionsBuilder.UseSqlServer(connectionString);

            return new MyTeamup(optionsBuilder.Options);
        }
    }
}
