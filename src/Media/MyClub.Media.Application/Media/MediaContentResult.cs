// -----------------------------------------------------------------------
// <copyright file="MediaContentResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Application.Media;

/// <summary>
/// Opened Media binary content.
/// </summary>
/// <param name="Content">Readable stream owned by the caller.</param>
/// <param name="ContentType">Content type for HTTP responses.</param>
/// <param name="ByteSize">Payload size in bytes.</param>
/// <param name="OriginalName">Optional original file name.</param>
public sealed record MediaContentResult(
    Stream Content,
    string ContentType,
    long ByteSize,
    string? OriginalName) : IAsyncDisposable, IDisposable
{
    /// <inheritdoc />
    public void Dispose() => Content.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
