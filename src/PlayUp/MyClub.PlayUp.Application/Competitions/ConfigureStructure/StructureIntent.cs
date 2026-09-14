// -----------------------------------------------------------------------
// <copyright file="StructureIntent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application command describing a typed V1 structure configuration.
/// </summary>
/// <remarks>
/// Not a Domain concept. Factories validate format-specific parameters before Domain mutation.
/// </remarks>
public sealed class StructureIntent
{
    private StructureIntent(
        StructureFormatKind format,
        string stageName,
        int matchdayCount,
        int groupCount,
        int participantsPerGroup,
        int bracketSize,
        MatchGenerationFormat matchGenerationFormat,
        int swissRoundCount)
    {
        Format = format;
        StageName = stageName;
        MatchdayCount = matchdayCount;
        GroupCount = groupCount;
        ParticipantsPerGroup = participantsPerGroup;
        BracketSize = bracketSize;
        MatchGenerationFormat = matchGenerationFormat;
        SwissRoundCount = swissRoundCount;
    }

    /// <summary>Gets the format kind.</summary>
    public StructureFormatKind Format { get; }

    /// <summary>Gets the primary stage display name.</summary>
    public string StageName { get; }

    /// <summary>Gets championship matchday count (Championship only).</summary>
    public int MatchdayCount { get; }

    /// <summary>Gets group count (Groups only).</summary>
    public int GroupCount { get; }

    /// <summary>Gets places per group (Groups only) — maps to Stage.PlacesPerGroup.</summary>
    public int ParticipantsPerGroup { get; }

    /// <summary>Gets cup bracket size — power of two (Cup only).</summary>
    public int BracketSize { get; }

    /// <summary>
    /// Gets how Championship / Groups matches are generated (Cup / Swiss ignore this).
    /// </summary>
    public MatchGenerationFormat MatchGenerationFormat { get; }

    /// <summary>Gets planned Swiss round count K (Swiss only).</summary>
    public int SwissRoundCount { get; }

    /// <summary>
    /// Builds a championship intent (matchdays only; fixtures/matches deferred to Slice 3).
    /// </summary>
    /// <param name="matchdayCount">Number of matchdays (≥ 1).</param>
    /// <param name="stageName">Optional stage name.</param>
    /// <param name="matchGenerationFormat">Single or double round-robin (default single).</param>
    /// <returns>Validated intent.</returns>
    public static StructureIntent Championship(
        int matchdayCount = 1,
        string? stageName = null,
        MatchGenerationFormat matchGenerationFormat = MatchGenerationFormat.SingleRoundRobin) =>
        matchdayCount < 1
            ? throw new ApplicationFailureException(
                "Championship requires at least one matchday.",
                ApplicationErrorCodes.InvalidStructureIntent)
            : !Enum.IsDefined(matchGenerationFormat)
            ? throw new ApplicationFailureException(
                $"Unknown match generation format '{matchGenerationFormat}'.",
                ApplicationErrorCodes.InvalidStructureIntent)
            : new StructureIntent(
                StructureFormatKind.Championship,
                NormalizeStageName(stageName, "Championnat"),
                matchdayCount,
                groupCount: 0,
                participantsPerGroup: 0,
                bracketSize: 0,
                matchGenerationFormat,
                swissRoundCount: 0);

    /// <summary>
    /// Builds a groups intent (empty groups + matchday + PlacesPerGroup form fact + Draw PotRules derived from it).
    /// </summary>
    /// <param name="groupCount">Number of groups (≥ 2).</param>
    /// <param name="participantsPerGroup">
    /// Places per group (≥ 2) — stored as <c>Stage.PlacesPerGroup</c>; also seeds PotRules (one-way).
    /// </param>
    /// <param name="stageName">Optional stage name.</param>
    /// <param name="matchGenerationFormat">Single or double round-robin (default single).</param>
    /// <returns>Validated intent.</returns>
    public static StructureIntent Groups(
        int groupCount,
        int participantsPerGroup,
        string? stageName = null,
        MatchGenerationFormat matchGenerationFormat = MatchGenerationFormat.SingleRoundRobin) =>
        groupCount < 2
            ? throw new ApplicationFailureException(
                "Groups format requires at least two groups.",
                ApplicationErrorCodes.InvalidStructureIntent)
            : participantsPerGroup < 2
                ? throw new ApplicationFailureException(
                    "Groups format requires at least two places per group.",
                    ApplicationErrorCodes.InvalidStructureIntent)
                : !Enum.IsDefined(matchGenerationFormat)
                ? throw new ApplicationFailureException(
                    $"Unknown match generation format '{matchGenerationFormat}'.",
                    ApplicationErrorCodes.InvalidStructureIntent)
                : new StructureIntent(
                    StructureFormatKind.Groups,
                    NormalizeStageName(stageName, "Phase de groupes"),
                    matchdayCount: 1,
                    groupCount,
                    participantsPerGroup,
                    bracketSize: 0,
                    matchGenerationFormat,
                    swissRoundCount: 0);

    /// <summary>
    /// Builds a cup intent. V1 bounds bracket size to a power of two (no bye matrix).
    /// </summary>
    /// <param name="bracketSize">Slot count; must be a power of two in [2, 64].</param>
    /// <param name="stageName">Optional stage name.</param>
    /// <returns>Validated intent.</returns>
    public static StructureIntent Cup(int bracketSize, string? stageName = null) =>
        bracketSize is < 2 or > 64 || !IsPowerOfTwo(bracketSize)
            ? throw new ApplicationFailureException(
                "Cup V1 requires a bracket size that is a power of two between 2 and 64 (non-power-of-two cups are out of scope).",
                ApplicationErrorCodes.CupBracketNotPowerOfTwo)
            : new StructureIntent(
                StructureFormatKind.Cup,
                NormalizeStageName(stageName, "Coupe"),
                matchdayCount: 0,
                groupCount: 0,
                participantsPerGroup: 0,
                bracketSize,
                MatchGenerationFormat.SingleRoundRobin,
                swissRoundCount: 0);

    /// <summary>
    /// Builds a Swiss intent (SwissSettings only — Matchdays created by GenerateNextRound).
    /// </summary>
    /// <param name="roundCount">Planned Swiss rounds K (≥ 1).</param>
    /// <param name="stageName">Optional stage name.</param>
    /// <returns>Validated intent.</returns>
    public static StructureIntent Swiss(int roundCount, string? stageName = null) =>
        roundCount < 1
            ? throw new ApplicationFailureException(
                "Swiss format requires at least one round (SwissSettings.RoundCount).",
                ApplicationErrorCodes.InvalidStructureIntent)
            : new StructureIntent(
                StructureFormatKind.Swiss,
                NormalizeStageName(stageName, "Swiss"),
                matchdayCount: 0,
                groupCount: 0,
                participantsPerGroup: 0,
                bracketSize: 0,
                MatchGenerationFormat.SingleRoundRobin,
                roundCount);

    private static string NormalizeStageName(string? stageName, string fallback) => string.IsNullOrWhiteSpace(stageName) ? fallback : stageName.Trim();

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
