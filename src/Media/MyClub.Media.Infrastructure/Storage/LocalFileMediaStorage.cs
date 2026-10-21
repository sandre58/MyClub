// -----------------------------------------------------------------------
// <copyright file="LocalFileMediaStorage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Media.Application.Abstractions;

namespace MyClub.Media.Infrastructure.Storage;

/// <summary>
/// Stores Media binaries on the local filesystem under a configured root directory.
/// </summary>
public sealed class LocalFileMediaStorage : IMediaStorage
{
    private readonly string _storageRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileMediaStorage"/> class.
    /// </summary>
    /// <param name="storageRoot">Absolute root directory for Media files.</param>
    public LocalFileMediaStorage(string storageRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRoot);
        _storageRoot = Path.GetFullPath(storageRoot);
        Directory.CreateDirectory(_storageRoot);
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentNullException.ThrowIfNull(content);
        _ = contentType;

        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var file = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using (file.ConfigureAwait(false))
        {
            await content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        cancellationToken.ThrowIfCancellationRequested();

        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Media file '{storageKey}' was not found.", path);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        cancellationToken.ThrowIfCancellationRequested();

        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    private string ResolvePath(string storageKey)
    {
        // Keys are always generated as "{guid:N}.ext" — reject path traversal.
        if (storageKey.Contains("..", StringComparison.Ordinal)
            || storageKey.Contains('/', StringComparison.Ordinal)
            || storageKey.Contains('\\', StringComparison.Ordinal)
            || Path.IsPathRooted(storageKey))
        {
            throw new ArgumentException("Storage key must be a simple file name.", nameof(storageKey));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, storageKey));
        return !fullPath.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase) ? throw new ArgumentException("Storage key resolves outside the storage root.", nameof(storageKey)) : fullPath;
    }
}
