// -----------------------------------------------------------------------
// <copyright file="PlayUpWebApplicationFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MyClub.PlayUp.Host.Tests;

internal sealed class PlayUpWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("ConnectionStrings:PlayUp", connectionString);
        builder.UseSetting("ConnectionStrings:Media", connectionString);
        builder.UseSetting("Media:StorageRoot", Path.Combine(Path.GetTempPath(), "myclub-media-host-tests", Guid.NewGuid().ToString("N")));
        builder.UseEnvironment("Development");
    }
}
