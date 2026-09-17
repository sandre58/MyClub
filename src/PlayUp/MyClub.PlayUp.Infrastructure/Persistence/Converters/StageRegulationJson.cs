// -----------------------------------------------------------------------
// <copyright file="StageRegulationJson.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Deterministic System.Text.Json options for persisting <see cref="StageRegulation"/> as JSONB.
/// </summary>
/// <remarks>
/// Options are independent from <see cref="RegulationJson.Options"/> so Stage-only converters
/// (<see cref="DrawConstraint"/>, <see cref="QualificationCondition"/>, typed ids) do not affect Competition regulation.
/// </remarks>
internal static class StageRegulationJson
{
    internal static readonly JsonSerializerOptions Options = CreateOptions();

    internal static string Serialize(StageRegulation regulation) => JsonSerializer.Serialize(regulation, Options);

    internal static StageRegulation Deserialize(string json) =>
        JsonSerializer.Deserialize<StageRegulation>(json, Options)
        ?? throw new InvalidOperationException("StageRegulation JSON is empty.");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new DrawConstraintJsonConverter());
        options.Converters.Add(new QualificationConditionJsonConverter());
        options.Converters.Add(new QualificationRulesJsonConverter());
        options.Converters.Add(new ProgressionRulesJsonConverter());
        options.Converters.Add(new GuidTypedIdJsonConverter<FixtureId>(static value => new FixtureId(value), static id => id.Value));
        options.Converters.Add(new GuidTypedIdJsonConverter<StageId>(static value => new StageId(value), static id => id.Value));
        options.Converters.Add(new GuidTypedIdJsonConverter<GroupId>(static value => new GroupId(value), static id => id.Value));
        options.Converters.Add(new GuidTypedIdJsonConverter<IntentId>(static value => new IntentId(value), static id => id.Value));
        options.Converters.Add(new GuidTypedIdJsonConverter<RoundId>(static value => new RoundId(value), static id => id.Value));
        return options;
    }
}
