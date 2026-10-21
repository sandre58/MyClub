// -----------------------------------------------------------------------
// <copyright file="PlayUpWebApplicationFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Media.Application.Abstractions;
using MyClub.Media.Infrastructure.Storage;

namespace MyClub.PlayUp.Host.Tests;

internal sealed class PlayUpWebApplicationFactory(string connectionString, bool failMediaDelete = false)
    : WebApplicationFactory<Program>
{
    private readonly string _mediaStorageRoot = Path.Combine(
        Path.GetTempPath(),
        "myclub-media-host-tests",
        Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("ConnectionStrings:PlayUp", connectionString);
        builder.UseSetting("ConnectionStrings:Media", connectionString);
        builder.UseSetting("Media:StorageRoot", _mediaStorageRoot);
        builder.UseEnvironment("Development");

        if (!failMediaDelete)
        {
            return;
        }

        var storageRoot = _mediaStorageRoot;
        builder.ConfigureTestServices(services =>
        {
            var existing = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(IMediaStorage));
            if (existing is not null)
            {
                _ = services.Remove(existing);
            }

            var inner = new LocalFileMediaStorage(storageRoot);
            services.AddSingleton<IMediaStorage>(new DeleteFailingMediaStorage(inner));
        });
    }

    /// <summary>
    /// Forwards save/open to the real local store but always fails delete (mapped HTTP 500).
    /// </summary>
    private sealed class DeleteFailingMediaStorage(IMediaStorage inner) : IMediaStorage
    {
        public Task SaveAsync(
            string storageKey,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default) =>
            inner.SaveAsync(storageKey, content, contentType, cancellationToken);

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            inner.OpenReadAsync(storageKey, cancellationToken);

        public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            throw new IOException($"Simulated storage delete failure for '{storageKey}'.");
    }
}
