// -----------------------------------------------------------------------
// <copyright file="PlayUpDatabaseHealthCheck.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyClub.PlayUp.Infrastructure.Persistence;

namespace MyClub.PlayUp.Host;

internal sealed class PlayUpDatabaseHealthCheck(PlayUpDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database
                .CanConnectAsync(cancellationToken)
                .ConfigureAwait(false);
            return canConnect
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("PlayUp database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PlayUp database health check failed.", exception);
        }
    }
}
