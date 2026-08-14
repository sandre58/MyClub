// -----------------------------------------------------------------------
// <copyright file="MatchResultJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Deterministic System.Text.Json serialization for <see cref="MatchResult"/> as JSONB via an Infrastructure DTO.
/// </summary>
/// <remarks>
/// Domain <see cref="Score"/> / <see cref="PenaltyShootoutScore"/> are get-only structs; STJ cannot reliably
/// rehydrate them through the Domain constructors, so persistence uses a flat document shape.
/// </remarks>
internal static class MatchResultJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string Serialize(MatchResult result) =>
        JsonSerializer.Serialize(MatchResultDocument.From(result), Options);

    internal static MatchResult Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<MatchResultDocument>(json, Options)
            ?? throw new InvalidOperationException("MatchResult JSON is empty.");
        return document.ToDomain();
    }

    private sealed record MatchResultDocument(
        ResultType Type,
        int HomeGoals,
        int AwayGoals,
        bool ExtraTimePlayed,
        int? PenaltyHomeGoals,
        int? PenaltyAwayGoals)
    {
        internal static MatchResultDocument From(MatchResult result) =>
            new(
                result.Type,
                result.Score.HomeGoals,
                result.Score.AwayGoals,
                result.ExtraTimePlayed,
                result.PenaltyShootoutScore?.HomeGoals,
                result.PenaltyShootoutScore?.AwayGoals);

        internal MatchResult ToDomain()
        {
            PenaltyShootoutScore? shootout = PenaltyHomeGoals is { } home && PenaltyAwayGoals is { } away
                ? new PenaltyShootoutScore(home, away)
                : null;

            return new MatchResult(Type, new Score(HomeGoals, AwayGoals), ExtraTimePlayed, shootout);
        }
    }
}
