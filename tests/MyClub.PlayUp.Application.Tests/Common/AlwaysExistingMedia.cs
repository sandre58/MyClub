// -----------------------------------------------------------------------
// <copyright file="AlwaysExistingMedia.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;

namespace MyClub.PlayUp.Application.Tests.Common;

/// <summary>
/// Test double: every Media Guid is considered present.
/// </summary>
internal sealed class AlwaysExistingMedia : IMediaReferenceChecker
{
    public static AlwaysExistingMedia Instance { get; } = new();

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
