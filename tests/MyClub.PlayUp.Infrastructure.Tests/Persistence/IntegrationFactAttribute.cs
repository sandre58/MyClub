// -----------------------------------------------------------------------
// <copyright file="IntegrationFactAttribute.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IO.Pipes;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (!IsDockerAvailable())
        {
            Skip = "Docker is required for PostgreSQL integration tests.";
        }
    }

    private static bool IsDockerAvailable()
    {
        if (!OperatingSystem.IsWindows()) return File.Exists("/var/run/docker.sock");
        try
        {
            using var client = new NamedPipeClientStream(".", "docker_engine", PipeDirection.InOut, PipeOptions.Asynchronous);
            client.Connect(200);
            return client.IsConnected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
