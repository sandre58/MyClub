// -----------------------------------------------------------------------
// <copyright file="DrawResolutionJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Deterministic JSON for <see cref="DrawResolution"/> via an Infrastructure document and Domain factories.
/// </summary>
internal static class DrawResolutionJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string Serialize(DrawResolution resolution) =>
        JsonSerializer.Serialize(DrawResolutionDocument.From(resolution), Options);

    internal static DrawResolution Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<DrawResolutionDocument>(json, Options)
            ?? throw new InvalidOperationException("DrawResolution JSON is empty.");
        return document.ToDomain();
    }

    private sealed record SlotPlacementDocument(Guid EntryId, string SlotKey);

    private sealed record GroupPlacementDocument(Guid EntryId, Guid GroupId);

    private sealed record PairingDocument(Guid EntryA, Guid EntryB);

    private sealed record DrawResolutionDocument(
        DrawResolutionState State,
        DrawResolutionKind? ResolvedKind,
        SlotPlacementDocument[]? SlotResults,
        GroupPlacementDocument[]? GroupResults,
        PairingDocument[]? PairingResults)
    {
        internal static DrawResolutionDocument From(DrawResolution resolution)
        {
            SlotPlacementDocument[]? slotResults = resolution.SlotResults.Count == 0
                ? null
                : [.. resolution.SlotResults.Select(p => new SlotPlacementDocument(p.EntryId.Value, p.SlotKey))];
            GroupPlacementDocument[]? groupResults = resolution.GroupResults.Count == 0
                ? null
                : [.. resolution.GroupResults.Select(p => new GroupPlacementDocument(p.EntryId.Value, p.GroupId.Value))];
            PairingDocument[]? pairingResults = resolution.PairingResults.Count == 0
                ? null
                : [.. resolution.PairingResults.Select(p => new PairingDocument(p.EntryA.Value, p.EntryB.Value))];
            return new DrawResolutionDocument(resolution.State, resolution.ResolvedKind, slotResults, groupResults, pairingResults);
        }

        internal DrawResolution ToDomain() =>
            State switch
            {
                DrawResolutionState.NotResolved => DrawResolution.NotResolved(),
                DrawResolutionState.NoSolution => DrawResolution.NoSolution(),
                DrawResolutionState.Resolved => ResolvedKind switch
                {
                    DrawResolutionKind.Slot => DrawResolution.ResolvedSlots(
                        [.. (SlotResults ?? []).Select(p => new SlotDrawPlacement(new EntryId(p.EntryId), p.SlotKey))]),
                    DrawResolutionKind.Group => DrawResolution.ResolvedGroups(
                    [
                        .. (GroupResults ?? []).Select(p =>
                            new GroupDrawPlacement(new EntryId(p.EntryId), new GroupId(p.GroupId)))
                    ]),
                    DrawResolutionKind.Pairing => DrawResolution.ResolvedPairings(
                    [
                        .. (PairingResults ?? []).Select(p =>
                            new PairingDrawResult(new EntryId(p.EntryA), new EntryId(p.EntryB)))
                    ]),
                    _ => throw new InvalidOperationException("Resolved draw resolution is missing ResolvedKind.")
                },
                _ => throw new InvalidOperationException($"Unknown draw resolution state '{State}'.")
            };
    }
}
