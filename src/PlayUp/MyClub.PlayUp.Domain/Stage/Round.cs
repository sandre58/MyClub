// -----------------------------------------------------------------------
// <copyright file="Round.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// An elimination round within a stage. Carries <see cref="TieFormat"/> only (no MatchRules).
/// </summary>
[DebuggerDisplay("{Name}")]
public sealed class Round : Entity<RoundId>
{
    /// <summary>
    /// Maximum allowed length of a round name after trim.
    /// </summary>
    public const int NameMaxLength = 100;

    private readonly List<Fixture> _fixtures = [];

    internal Round(RoundId id, string name, TieFormat? tieFormat)
        : base(id)
    {
        Name = NormalizeName(name);
        TieFormat = tieFormat;
    }

    /// <summary>
    /// Gets the round display name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the tie format for confrontations in this round, if any.
    /// </summary>
    public TieFormat? TieFormat { get; private set; }

    /// <summary>
    /// Gets the fixtures belonging to this round.
    /// </summary>
    public IReadOnlyList<Fixture> Fixtures => _fixtures.AsReadOnly();

    internal void Rename(string name)
    {
        var normalized = NormalizeName(name);
        if (string.Equals(Name, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Name = normalized;
    }

    internal void ReplaceTieFormat(TieFormat? tieFormat) => TieFormat = tieFormat;

    internal void AddFixture(Fixture fixture) => _fixtures.Add(fixture);

    internal bool RemoveFixture(FixtureId fixtureId)
    {
        var index = _fixtures.FindIndex(f => f.Id.Equals(fixtureId));
        if (index < 0)
        {
            return false;
        }

        _fixtures.RemoveAt(index);
        return true;
    }

    internal Fixture? FindFixture(FixtureId fixtureId) =>
        _fixtures.FirstOrDefault(f => f.Id.Equals(fixtureId));

    private static string NormalizeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var trimmed = name.Trim();
        return trimmed.Length switch
        {
            0 => throw new DomainException("Round name cannot be empty.", StageErrorCodes.InvalidDisplayName),
            > NameMaxLength => throw new DomainException(
                $"Round name cannot exceed {NameMaxLength} characters.",
                StageErrorCodes.InvalidDisplayName),
            _ => trimmed
        };
    }
}
