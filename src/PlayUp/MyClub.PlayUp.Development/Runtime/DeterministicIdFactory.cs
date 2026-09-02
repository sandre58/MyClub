// -----------------------------------------------------------------------
// <copyright file="DeterministicIdFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Produces stable name-based Guids (UUID v5) scoped to a scenario run (<c>scenarioId</c> + workspace seed).
/// </summary>
public sealed class DeterministicIdFactory
{
    /// <summary>
    /// Fixed namespace for Play'Up Development Workspace identity derivation (RFC 4122 DNS namespace).
    /// </summary>
    private static readonly Guid PlayUpDevNamespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    private readonly Guid _runNamespace;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicIdFactory"/> class.
    /// </summary>
    /// <param name="scenarioId">Scenario identity.</param>
    /// <param name="workspaceSeed">Workspace seed from configuration.</param>
    public DeterministicIdFactory(string scenarioId, int workspaceSeed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        _runNamespace = CreateVersion5(PlayUpDevNamespace, $"{scenarioId}\0{workspaceSeed}");
    }

    /// <summary>
    /// Creates a name-based Guid for the given kind and local key.
    /// </summary>
    /// <param name="kind">Identity kind (competition, stage, match, team, entry, …).</param>
    /// <param name="localKey">Stable local key within the scenario.</param>
    /// <returns>A non-empty Guid.</returns>
    public Guid Create(string kind, string localKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(localKey);
        return CreateVersion5(_runNamespace, $"{kind}\0{localKey}");
    }

    /// <summary>Creates a deterministic <see cref="CompetitionId"/>.</summary>
    /// <param name="localKey">Local key (default <c>competition</c>).</param>
    /// <returns>Typed identity.</returns>
    public CompetitionId Competition(string localKey = "competition") => new(Create("competition", localKey));

    /// <summary>Creates a deterministic <see cref="StageId"/>.</summary>
    /// <param name="localKey">Local key (default <c>stage</c>).</param>
    /// <returns>Typed identity.</returns>
    public StageId Stage(string localKey = "stage") => new(Create("stage", localKey));

    /// <summary>Creates a deterministic <see cref="MatchId"/>.</summary>
    /// <param name="localKey">Local key.</param>
    /// <returns>Typed identity.</returns>
    public MatchId Match(string localKey) => new(Create("match", localKey));

    /// <summary>Creates a deterministic <see cref="TeamId"/>.</summary>
    /// <param name="localKey">Local key.</param>
    /// <returns>Typed identity.</returns>
    public TeamId Team(string localKey) => new(Create("team", localKey));

    /// <summary>Creates a deterministic <see cref="EntryId"/>.</summary>
    /// <param name="localKey">Local key.</param>
    /// <returns>Typed identity.</returns>
    public EntryId Entry(string localKey) => new(Create("entry", localKey));

    /// <summary>Creates a deterministic <see cref="DrawId"/>.</summary>
    /// <param name="localKey">Local key.</param>
    /// <returns>Typed identity.</returns>
    public DrawId Draw(string localKey = "draw") => new(Create("draw", localKey));

    /// <summary>Creates a deterministic <see cref="MemberId"/>.</summary>
    /// <param name="localKey">Local key.</param>
    /// <returns>Typed identity.</returns>
    public MemberId Member(string localKey) => new(Create("member", localKey));

    /// <summary>Creates a deterministic <see cref="ResourceId"/>.</summary>
    /// <param name="localKey">Local key (default <c>resource</c>).</param>
    /// <returns>Typed identity.</returns>
    public ResourceId Resource(string localKey = "resource") => new(Create("resource", localKey));

    /// <summary>
    /// RFC 4122 UUID version 5 (SHA-1 name-based).
    /// </summary>
    private static Guid CreateVersion5(Guid namespaceId, string name)
    {
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var data = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, data, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, data, namespaceBytes.Length, nameBytes.Length);

#pragma warning disable CA5350 // SHA-1 is required by RFC 4122 UUID v5.
        var hash = SHA1.HashData(data);
#pragma warning restore CA5350
        var newGuid = new byte[16];
        Array.Copy(hash, 0, newGuid, 0, 16);

        newGuid[6] = (byte)((newGuid[6] & 0x0F) | 0x50);
        newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);
        SwapByteOrder(newGuid);
        return new Guid(newGuid);
    }

    private static void SwapByteOrder(byte[] guid)
    {
        Swap(guid, 0, 3);
        Swap(guid, 1, 2);
        Swap(guid, 4, 5);
        Swap(guid, 6, 7);
    }

    private static void Swap(byte[] array, int left, int right) =>
        (array[left], array[right]) = (array[right], array[left]);
}
