// -----------------------------------------------------------------------
// <copyright file="PlayUpInfrastructureServiceCollectionExtensionsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Time;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.DependencyInjection;

public sealed class PlayUpInfrastructureServiceCollectionExtensionsTests
{
    private const string ConnectionString = "Host=127.0.0.1;Database=playup_unconnected;Username=x;Password=x";

    [Fact]
    public void AddPlayUpInfrastructure_registers_context_as_unit_of_work_and_system_clock()
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(ConnectionString);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        var context = scoped.GetRequiredService<PlayUpDbContext>();
        var unitOfWork = scoped.GetRequiredService<IUnitOfWork>();
        var competitionRepository = scoped.GetRequiredService<ICompetitionRepository>();
        var matchRepository = scoped.GetRequiredService<IMatchRepository>();
        var clock = scoped.GetRequiredService<IClock>();

        unitOfWork.Should().BeSameAs(context);
        competitionRepository.Should().NotBeNull();
        matchRepository.Should().NotBeNull();
        clock.Should().BeOfType<SystemClock>();
        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");

        using var otherScope = provider.CreateScope();
        otherScope.ServiceProvider.GetRequiredService<IClock>().Should().BeSameAs(clock);
        otherScope.ServiceProvider.GetRequiredService<PlayUpDbContext>().Should().NotBeSameAs(context);
    }
}
