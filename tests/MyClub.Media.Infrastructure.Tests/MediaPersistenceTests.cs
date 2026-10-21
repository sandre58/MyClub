// -----------------------------------------------------------------------
// <copyright file="MediaPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyClub.Media.Application;
using MyClub.Media.Application.Media;
using MyClub.Media.Domain;
using MyClub.Media.Infrastructure.Persistence;
using Xunit;

namespace MyClub.Media.Infrastructure.Tests;

[Collection(MediaPostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MediaPersistenceTests(MediaPostgresFixture fixture)
{
    [Fact]
    public async Task CreateGetContentDelete_PersistsMetadataAndFileAsync()
    {
        using var scope = fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<MediaService>();
        var payload = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A };

        MediaMetadataDto metadata;
        await using (var input = new MemoryStream(payload))
        {
            metadata = await service.CreateAsync(input, MediaContentTypes.Png, payload.Length, "crest.png");
        }

        metadata.ContentType.Should().Be(MediaContentTypes.Png);
        metadata.ByteSize.Should().Be(payload.Length);

        var loaded = await service.GetMetadataAsync(new MediaId(metadata.Id));
        loaded.Id.Should().Be(metadata.Id);

        await using (var content = await service.OpenContentAsync(new MediaId(metadata.Id)))
        {
            content.ContentType.Should().Be(MediaContentTypes.Png);
            await using var buffer = new MemoryStream();
            await content.Content.CopyToAsync(buffer);
            buffer.ToArray().Should().Equal(payload);
        }

        Directory.EnumerateFiles(fixture.StorageRoot).Should().NotBeEmpty();

        await service.DeleteAsync(new MediaId(metadata.Id));

        Directory.EnumerateFiles(fixture.StorageRoot).Should().BeEmpty();

        var act = () => service.GetMetadataAsync(new MediaId(metadata.Id));
        await act.Should().ThrowAsync<ApplicationFailureException>();
    }

    [Fact]
    public void Schema_IsIsolatedUnderMedia()
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var entityType = context.Model.FindEntityType(typeof(MediaItem));

        entityType.Should().NotBeNull();
        entityType.GetSchema().Should().Be("media");
        entityType.GetTableName().Should().Be("media_items");
    }
}
