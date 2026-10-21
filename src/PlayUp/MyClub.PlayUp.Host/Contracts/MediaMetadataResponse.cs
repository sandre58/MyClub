// -----------------------------------------------------------------------
// <copyright file="MediaMetadataResponse.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP response body for Media metadata.
/// </summary>
/// <param name="Id">Media identity.</param>
/// <param name="ContentType">Validated content type.</param>
/// <param name="ByteSize">Payload size in bytes.</param>
/// <param name="OriginalName">Optional original file name.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
public sealed record MediaMetadataResponse(
    Guid Id,
    string ContentType,
    long ByteSize,
    string? OriginalName,
    DateTimeOffset CreatedAt);
