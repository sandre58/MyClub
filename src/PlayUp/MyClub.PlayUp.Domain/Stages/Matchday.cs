// -----------------------------------------------------------------------
// <copyright file="Matchday.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// A matchday (journée) within a stage, holding fixtures for championship or poule calendars.
/// </summary>
[DebuggerDisplay("Matchday {Number}")]
public sealed class Matchday : Entity<MatchdayId>
{
    private readonly List<Fixture> _fixtures = [];

    internal Matchday(MatchdayId id, int number)
        : base(id)
    {
        if (number < 1)
        {
            throw new DomainException(
                "Matchday number must be greater than or equal to 1.",
                StageErrorCodes.InvalidConfiguration);
        }

        Number = number;
    }

    /// <summary>
    /// Gets the matchday number (1-based).
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// Gets the fixtures belonging to this matchday.
    /// </summary>
    public IReadOnlyList<Fixture> Fixtures => _fixtures.AsReadOnly();

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
}
