// -----------------------------------------------------------------------
// <copyright file="MediaItemTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace MyClub.Media.Domain.Tests;

public sealed class MediaItemTests
{
    [Fact]
    public void Create_WithValidPng_Succeeds()
    {
        var media = MediaItem.Create(MediaContentTypes.Png, 128, "crest.png", DateTimeOffset.UtcNow);

        media.ContentType.Should().Be(MediaContentTypes.Png);
        media.ByteSize.Should().Be(128);
        media.OriginalName.Should().Be("crest.png");
        media.StorageKey.Should().EndWith(".png");
        media.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithDisallowedContentType_Throws()
    {
        var act = () => MediaItem.Create("image/svg+xml", 128, "x.svg", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MediaErrorCodes.InvalidContentType);
    }

    [Fact]
    public void Create_WithOversizedPayload_Throws()
    {
        var act = () => MediaItem.Create(MediaContentTypes.Jpeg, MediaPolicies.MaxByteSize + 1, "big.jpg", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MediaErrorCodes.PayloadTooLarge);
    }

    [Fact]
    public void MediaId_Empty_Throws()
    {
        var act = () => new MediaId(Guid.Empty);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MediaErrorCodes.InvalidMediaId);
    }
}
