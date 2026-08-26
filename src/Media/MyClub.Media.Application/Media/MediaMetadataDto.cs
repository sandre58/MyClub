// -----------------------------------------------------------------------
// <copyright file="MediaMetadataDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Application.Media;

/// <summary>
/// Public Media metadata returned by application use cases.
/// </summary>
/// <param name="Id">Media identity (Guid).</param>
/// <param name="ContentType">Validated content type.</param>
/// <param name="ByteSize">Payload size in bytes.</param>
/// <param name="OriginalName">Optional sanitized original file name.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
public sealed record MediaMetadataDto(
    Guid Id,
    string ContentType,
    long ByteSize,
    string? OriginalName,
    DateTimeOffset CreatedAt);
