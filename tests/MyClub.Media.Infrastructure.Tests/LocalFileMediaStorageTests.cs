// -----------------------------------------------------------------------
// <copyright file="LocalFileMediaStorageTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MyClub.Media.Domain;
using MyClub.Media.Infrastructure.Storage;
using Xunit;

namespace MyClub.Media.Infrastructure.Tests;

public sealed class LocalFileMediaStorageTests
{
    [Fact]
    public async Task SaveOpenDelete_RoundTripsBytesAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "myclub-media-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalFileMediaStorage(root);
            var key = $"{Guid.CreateVersion7():N}.png";
            var payload = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

            await using (var input = new MemoryStream(payload))
            {
                await storage.SaveAsync(key, input, MediaContentTypes.Png);
            }

            await using (var output = await storage.OpenReadAsync(key))
            {
                await using var buffer = new MemoryStream();
                await output.CopyToAsync(buffer);
                buffer.ToArray().Should().Equal(payload);
            }

            (await storage.DeleteAsync(key)).Should().BeTrue();
            (await storage.DeleteAsync(key)).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test")]
    [SuppressMessage("Reliability", "CA2025:Do not pass 'IDisposable' instances into unawaited tasks", Justification = "Test")]
    public async Task Save_RejectsPathTraversalKeyAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "myclub-media-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalFileMediaStorage(root);
            await using var input = new MemoryStream([1, 2, 3]);

            var act = () => storage.SaveAsync("../escape.png", input, MediaContentTypes.Png);

            await act.Should().ThrowAsync<ArgumentException>();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
