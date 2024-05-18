// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyClub.DatabaseContext.Domain;
using MyClub.DatabaseContext.Infrastructure.Data;

namespace MyClub.Plugins.Teamup.Import.Database
{
    internal abstract class ImportDatabaseItemsPlugin<T> : IDisposable
    {
        private readonly UnitOfWork _unitOfWork;
        private bool _disposedValue;

        protected ImportDatabaseItemsPlugin()
        {
            // Configuration
            var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName)
                                .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
                                .Build();

            var connectionString = configuration.GetConnectionString("Default");
            var optionsBuilder = new DbContextOptionsBuilder<MyTeamup>();
            optionsBuilder.UseSqlServer(connectionString);
            _unitOfWork = new UnitOfWork(new MyTeamup(optionsBuilder.Options));
        }

        public bool CanImport() => _unitOfWork.CanConnect();

        public IEnumerable<T> ProvideItems() => LoadItems(_unitOfWork);

        public abstract IEnumerable<T> LoadItems(IUnitOfWork unitOfWork);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _unitOfWork.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
