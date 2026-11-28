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
/// Before reverting a release that persisted confrontation tie shootouts, run:
/// <c>SELECT id FROM matches WHERE result -&gt;&gt; 'ConfrontationTiePenaltyShootout' = 'true'</c>
/// and correct or clear those results.
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
        int? PenaltyAwayGoals,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        bool ConfrontationTiePenaltyShootout)
    {
        internal static MatchResultDocument From(MatchResult result) =>
            new(
                result.Type,
                result.Score.HomeGoals,
                result.Score.AwayGoals,
                result.ExtraTimePlayed,
                result.PenaltyShootoutScore?.HomeGoals,
                result.PenaltyShootoutScore?.AwayGoals,
                result.ConfrontationTiePenaltyShootout);

        internal MatchResult ToDomain()
        {
            PenaltyShootoutScore? shootout = PenaltyHomeGoals is { } home && PenaltyAwayGoals is { } away
                ? new PenaltyShootoutScore(home, away)
                : null;

            var score = new Score(HomeGoals, AwayGoals);
            if (ConfrontationTiePenaltyShootout)
            {
                if (Type != ResultType.Played)
                {
                    throw new DomainException(
                        "Confrontation tie penalty shootout is allowed only for played results.",
                        MatchErrorCodes.InvalidResult);
                }

                if (shootout is null)
                {
                    throw new DomainException(
                        "Confrontation tie penalty shootout requires a decisive shootout score.",
                        MatchErrorCodes.InvalidResult);
                }

                return MatchResult.CreateConfrontationTiePenaltyShootout(score, ExtraTimePlayed, shootout.Value);
            }

            return new MatchResult(Type, score, ExtraTimePlayed, shootout);
        }
    }
}
