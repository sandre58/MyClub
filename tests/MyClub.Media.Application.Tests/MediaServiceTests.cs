// -----------------------------------------------------------------------
// <copyright file="MediaServiceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.Media.Application;
using MyClub.Media.Application.Abstractions;
using MyClub.Media.Application.Media;
using MyClub.Media.Domain;
using Xunit;

namespace MyClub.Media.Application.Tests;

public sealed class MediaServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenRepositoryFails_DeletesStoredFile()
    {
        var repository = new Mock<IMediaRepository>(MockBehavior.Strict);
        var storage = new Mock<IMediaStorage>(MockBehavior.Strict);
        string? savedKey = null;

        storage
            .Setup(candidate => candidate.SaveAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                MediaContentTypes.Png,
                It.IsAny<CancellationToken>()))
            .Callback<string, Stream, string, CancellationToken>((key, _, _, _) => savedKey = key)
            .Returns(Task.CompletedTask);

        repository.Setup(candidate => candidate.Add(It.IsAny<MediaItem>()));
        repository
            .Setup(candidate => candidate.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        storage
            .Setup(candidate => candidate.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new MediaService(repository.Object, storage.Object);
        await using var stream = new MemoryStream([1, 2, 3, 4]);

        var act = () => service.CreateAsync(stream, MediaContentTypes.Png, 4, "a.png");

        await act.Should().ThrowAsync<InvalidOperationException>();
        savedKey.Should().NotBeNullOrWhiteSpace();
        storage.Verify(
            candidate => candidate.DeleteAsync(savedKey!, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RemovesMetadataBeforeFile()
    {
        var media = MediaItem.Create(MediaContentTypes.Webp, 8, "x.webp", DateTimeOffset.UtcNow);
        var repository = new Mock<IMediaRepository>(MockBehavior.Strict);
        var storage = new Mock<IMediaStorage>(MockBehavior.Strict);
        var sequence = new MockSequence();

        repository
            .InSequence(sequence)
            .Setup(candidate => candidate.GetByIdAsync(media.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(media);
        repository.InSequence(sequence).Setup(candidate => candidate.Remove(media));
        repository
            .InSequence(sequence)
            .Setup(candidate => candidate.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        storage
            .InSequence(sequence)
            .Setup(candidate => candidate.DeleteAsync(media.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new MediaService(repository.Object, storage.Object);

        await service.DeleteAsync(media.Id);

        repository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task GetMetadataAsync_WhenMissing_ThrowsNotFound()
    {
        var repository = new Mock<IMediaRepository>();
        var storage = new Mock<IMediaStorage>();
        repository
            .Setup(candidate => candidate.GetByIdAsync(It.IsAny<MediaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItem?)null);

        var service = new MediaService(repository.Object, storage.Object);

        var act = () => service.GetMetadataAsync(MediaId.New());

        var exception = await act.Should().ThrowAsync<ApplicationFailureException>();
        exception.Which.Code.Should().Be(MediaApplicationErrorCodes.MediaNotFound);
    }
}
