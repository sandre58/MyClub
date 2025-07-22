// -----------------------------------------------------------------------
// <copyright file="DependencyInjectionExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Scorer.Domain.CompetitionAggregate.Repositories;
using MyClub.Scorer.Domain.MatchAggregate.Repositories;
using MyClub.Scorer.Domain.MatchdayAggregate.Repositories;
using MyClub.Scorer.Domain.RoundAggregate.Repositories;
using MyClub.Scorer.Domain.StageAggregate.Repositories;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;

namespace MyClub.Scorer.Infrastructure.Persistence.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, Action<DbContextOptionsBuilder> dbOptions)
    {
        services.AddDbContext<MyClubDbContext>(dbOptions);

        // Repositories
        //services.AddScoped<ICompetitionRepository, EfCompetitionRepository>();
        //services.AddScoped<IMatchdayRepository, EfCompetitionRepository>();
        //services.AddScoped<IMatchRepository, EfCompetitionRepository>();
        //services.AddScoped<IRoundRepository, EfCompetitionRepository>();
        //services.AddScoped<IStageRepository, EfCompetitionRepository>();

        return services;
    }
}
