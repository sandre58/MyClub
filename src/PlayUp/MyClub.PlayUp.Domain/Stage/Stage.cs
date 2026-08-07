// -----------------------------------------------------------------------
// <copyright file="Stage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage.Events;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Aggregate root for a competition phase: lifecycle, structure, and materialized regulation.
/// </summary>
[DebuggerDisplay("{Name} ({Status})")]
public sealed class Stage : AggregateRoot<StageId>
{
    private readonly List<Group> _groups = [];
    private readonly List<Round> _rounds = [];
    private readonly List<Matchday> _matchdays = [];

    private Stage(StageId id, CompetitionId competitionId, StageName name, StageRegulation regulation)
        : base(id)
    {
        CompetitionId = competitionId;
        Name = name;
        Regulation = regulation;
        Status = StageStatus.Draft;
    }

    /// <summary>
    /// Gets the owning competition identity (immutable).
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the stage name.
    /// </summary>
    public StageName Name { get; private set; }

    /// <summary>
    /// Gets the materialized stage regulation (independent from the competition regulation).
    /// </summary>
    public StageRegulation Regulation { get; private set; }

    /// <summary>
    /// Gets the stage lifecycle status.
    /// </summary>
    public StageStatus Status { get; private set; }

    /// <summary>
    /// Gets the groups in this stage.
    /// </summary>
    public IReadOnlyList<Group> Groups => _groups.AsReadOnly();

    /// <summary>
    /// Gets the rounds in this stage.
    /// </summary>
    public IReadOnlyList<Round> Rounds => _rounds.AsReadOnly();

    /// <summary>
    /// Gets the matchdays in this stage.
    /// </summary>
    public IReadOnlyList<Matchday> Matchdays => _matchdays.AsReadOnly();

    private bool HasStructure => _groups.Count > 0 || _rounds.Count > 0 || _matchdays.Count > 0;

    /// <summary>
    /// Creates a new stage in Draft status with a regulation materialized from the competition.
    /// </summary>
    /// <param name="competitionId">The owning competition identity.</param>
    /// <param name="name">The stage name.</param>
    /// <param name="competitionRegulation">The competition regulation to materialize from.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created stage.</returns>
    public static Stage Create(
        CompetitionId competitionId,
        StageName name,
        Regulation competitionRegulation,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competitionRegulation);
        return Create(competitionId, name, StageRegulation.MaterializeFrom(competitionRegulation), clock);
    }

    /// <summary>
    /// Creates a new stage in Draft status with an independent copy of the given stage regulation.
    /// </summary>
    /// <param name="competitionId">The owning competition identity.</param>
    /// <param name="name">The stage name.</param>
    /// <param name="regulation">The stage regulation (cloned on create).</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created stage.</returns>
    public static Stage Create(
        CompetitionId competitionId,
        StageName name,
        StageRegulation regulation,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(clock);

        var stage = new Stage(StageId.New(), competitionId, name, regulation.Copy());
        stage.Raise(new StageCreated(stage.Id, competitionId, name.Value, clock));
        return stage;
    }

    /// <summary>
    /// Replaces the stage regulation as a whole (including match rules).
    /// Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    /// <param name="regulation">The new stage regulation.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceRegulation(StageRegulation regulation, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        Regulation = regulation.Copy();
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces standing rules only. Allowed after Start (calculation ≠ structure).
    /// </summary>
    /// <param name="standingRules">The new standing rules.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceStandingRules(StandingRules standingRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(standingRules);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStandingRulesMutable();

        Regulation = Regulation.WithStandingRules(standingRules);
        Raise(new StageStandingRulesReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces draw rules. Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    /// <param name="drawRules">The new draw rules, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceDrawRules(DrawRules? drawRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        Regulation = Regulation.WithDrawRules(drawRules);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces qualification rules. Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    /// <param name="qualificationRules">The new qualification rules, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceQualificationRules(QualificationRules? qualificationRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        Regulation = Regulation.WithQualificationRules(qualificationRules);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces the default stage tie format (source for new rounds). Allowed in Draft or Ready.
    /// </summary>
    /// <param name="tieFormat">The new default tie format, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceDefaultTieFormat(TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        Regulation = Regulation.WithTieFormat(tieFormat);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Gets a group by identity.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <returns>The group.</returns>
    public Group GetGroup(GroupId groupId) =>
        _groups.FirstOrDefault(g => g.Id.Equals(groupId))
        ?? throw new DomainException($"Group '{groupId}' was not found.", StageErrorCodes.GroupNotFound);

    /// <summary>
    /// Finds a group by identity, if any.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <returns>The group, or <see langword="null"/>.</returns>
    public Group? FindGroup(GroupId groupId) =>
        _groups.FirstOrDefault(g => g.Id.Equals(groupId));

    /// <summary>
    /// Determines whether a group is present.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool HasGroup(GroupId groupId) => FindGroup(groupId) is not null;

    /// <summary>
    /// Determines whether a round is present.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool HasRound(RoundId roundId) => _rounds.Any(r => r.Id.Equals(roundId));

    /// <summary>
    /// Determines whether a matchday is present.
    /// </summary>
    /// <param name="matchdayId">The matchday identity.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool HasMatchday(MatchdayId matchdayId) => _matchdays.Any(m => m.Id.Equals(matchdayId));

    /// <summary>
    /// Gets a fixture by identity.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <returns>The fixture.</returns>
    public Fixture GetFixture(FixtureId fixtureId) =>
        FindFixture(fixtureId)
        ?? throw new DomainException($"Fixture '{fixtureId}' was not found.", StageErrorCodes.FixtureNotFound);

    /// <summary>
    /// Finds a fixture by identity, if any.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <returns>The fixture, or <see langword="null"/>.</returns>
    public Fixture? FindFixture(FixtureId fixtureId) =>
        _rounds.Select(r => r.FindFixture(fixtureId)).FirstOrDefault(f => f is not null)
        ?? _matchdays.Select(m => m.FindFixture(fixtureId)).FirstOrDefault(f => f is not null);

    /// <summary>
    /// Determines whether a fixture is present.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool HasFixture(FixtureId fixtureId) => FindFixture(fixtureId) is not null;

    /// <summary>
    /// Determines whether a match identity is attached to any fixture in this stage.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <returns><see langword="true"/> if attached; otherwise, <see langword="false"/>.</returns>
    public bool HasMatch(MatchId matchId) =>
        _rounds.SelectMany(r => r.Fixtures).Any(f => f.Contains(matchId))
        || _matchdays.SelectMany(m => m.Fixtures).Any(f => f.Contains(matchId));

    /// <summary>
    /// Renames the stage. No-op when the normalized name is unchanged.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void Rename(StageName name, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        if (Name.Equals(name))
        {
            return;
        }

        Name = name;
    }

    /// <summary>
    /// Adds a group to the stage.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created group.</returns>
    public Group AddGroup(string name, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsureCanAddGroup();

        DemoteToDraftIfReady();
        var group = new Group(GroupId.New(), name);
        _groups.Add(group);
        Raise(new StageGroupAdded(Id, group.Id, clock));
        return group;
    }

    /// <summary>
    /// Removes a group from the stage.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveGroup(GroupId groupId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var group = GetGroup(groupId);
        DemoteToDraftIfReady();
        _groups.Remove(group);
        Raise(new StageGroupRemoved(Id, groupId, clock));
    }

    /// <summary>
    /// Renames a group. No-op when the normalized name is unchanged.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <param name="name">The new name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RenameGroup(GroupId groupId, string name, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        GetGroup(groupId).Rename(name);
    }

    /// <summary>
    /// Assigns an entry to a group. Application validates that the entry belongs to the competition.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void AssignEntryToGroup(GroupId groupId, EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var owningGroup = _groups.FirstOrDefault(g => g.Contains(entryId));
        if (owningGroup is not null)
        {
            if (owningGroup.Id.Equals(groupId))
            {
                return;
            }

            throw new DomainException(
                $"Entry '{entryId}' is already assigned to another group.",
                StageErrorCodes.DuplicateEntry);
        }

        var group = GetGroup(groupId);
        DemoteToDraftIfReady();
        group.Assign(entryId);
    }

    /// <summary>
    /// Removes an entry from a group.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveEntryFromGroup(GroupId groupId, EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var group = GetGroup(groupId);
        if (!group.Contains(entryId))
        {
            throw new DomainException(
                $"Entry '{entryId}' was not found in group '{groupId}'.",
                StageErrorCodes.EntryNotFound);
        }

        DemoteToDraftIfReady();
        group.Remove(entryId);
    }

    /// <summary>
    /// Arranges groups in the given order. No-op when the order is unchanged.
    /// </summary>
    /// <param name="orderedGroupIds">A permutation of current group identities.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ArrangeGroups(IReadOnlyList<GroupId> orderedGroupIds, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(orderedGroupIds);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsurePermutation(orderedGroupIds, _groups.ConvertAll(g => g.Id));

        if (_groups.Select(g => g.Id).SequenceEqual(orderedGroupIds))
        {
            return;
        }

        DemoteToDraftIfReady();
        var map = _groups.ToDictionary(g => g.Id);
        _groups.Clear();
        foreach (var id in orderedGroupIds)
        {
            _groups.Add(map[id]);
        }
    }

    /// <summary>
    /// Adds a round to the stage, materializing <see cref="StageRegulation.TieFormat"/> when present.
    /// </summary>
    /// <param name="name">The round name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created round.</returns>
    public Round AddRound(string name, IClock clock) =>
        AddRound(name, Regulation.TieFormat, clock);

    /// <summary>
    /// Adds a round with an explicit tie format (or <see langword="null"/> to omit one).
    /// </summary>
    /// <param name="name">The round name.</param>
    /// <param name="tieFormat">The tie format to materialize, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created round.</returns>
    public Round AddRound(string name, TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsureCanAddRound();

        DemoteToDraftIfReady();
        var round = new Round(RoundId.New(), name, tieFormat?.Copy());
        _rounds.Add(round);
        Raise(new StageRoundAdded(Id, round.Id, clock));
        return round;
    }

    /// <summary>
    /// Replaces the tie format of a round. Allowed before the stage structure is locked.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <param name="tieFormat">The new tie format, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceRoundTieFormat(RoundId roundId, TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var round = _rounds.FirstOrDefault(r => r.Id.Equals(roundId))
            ?? throw new DomainException($"Round '{roundId}' was not found.", StageErrorCodes.RoundNotFound);

        DemoteToDraftIfReady();
        round.ReplaceTieFormat(tieFormat?.Copy());
        Raise(new StageRoundTieFormatReplaced(Id, roundId, clock));
    }

    /// <summary>
    /// Removes a round from the stage.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveRound(RoundId roundId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var round = _rounds.FirstOrDefault(r => r.Id.Equals(roundId))
            ?? throw new DomainException($"Round '{roundId}' was not found.", StageErrorCodes.RoundNotFound);

        DemoteToDraftIfReady();
        _rounds.Remove(round);
        Raise(new StageRoundRemoved(Id, roundId, clock));
    }

    /// <summary>
    /// Renames a round. No-op when the normalized name is unchanged.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <param name="name">The new name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RenameRound(RoundId roundId, string name, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        var round = _rounds.FirstOrDefault(r => r.Id.Equals(roundId))
            ?? throw new DomainException($"Round '{roundId}' was not found.", StageErrorCodes.RoundNotFound);
        round.Rename(name);
    }

    /// <summary>
    /// Arranges rounds in the given order. No-op when the order is unchanged.
    /// </summary>
    /// <param name="orderedRoundIds">A permutation of current round identities.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ArrangeRounds(IReadOnlyList<RoundId> orderedRoundIds, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(orderedRoundIds);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsurePermutation(orderedRoundIds, _rounds.ConvertAll(r => r.Id));

        if (_rounds.Select(r => r.Id).SequenceEqual(orderedRoundIds))
        {
            return;
        }

        DemoteToDraftIfReady();
        var map = _rounds.ToDictionary(r => r.Id);
        _rounds.Clear();
        foreach (var id in orderedRoundIds)
        {
            _rounds.Add(map[id]);
        }
    }

    /// <summary>
    /// Adds a matchday to the stage.
    /// </summary>
    /// <param name="number">The 1-based matchday number.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created matchday.</returns>
    public Matchday AddMatchday(int number, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsureCanAddMatchday();

        DemoteToDraftIfReady();
        var matchday = new Matchday(MatchdayId.New(), number);
        _matchdays.Add(matchday);
        Raise(new StageMatchdayAdded(Id, matchday.Id, clock));
        return matchday;
    }

    /// <summary>
    /// Removes a matchday from the stage.
    /// </summary>
    /// <param name="matchdayId">The matchday identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveMatchday(MatchdayId matchdayId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var matchday = _matchdays.FirstOrDefault(m => m.Id.Equals(matchdayId))
            ?? throw new DomainException(
                $"Matchday '{matchdayId}' was not found.",
                StageErrorCodes.MatchdayNotFound);

        DemoteToDraftIfReady();
        _matchdays.Remove(matchday);
        Raise(new StageMatchdayRemoved(Id, matchdayId, clock));
    }

    /// <summary>
    /// Arranges matchdays in the given order. No-op when the order is unchanged.
    /// </summary>
    /// <param name="orderedMatchdayIds">A permutation of current matchday identities.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ArrangeMatchdays(IReadOnlyList<MatchdayId> orderedMatchdayIds, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(orderedMatchdayIds);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsurePermutation(orderedMatchdayIds, _matchdays.ConvertAll(m => m.Id));

        if (_matchdays.Select(m => m.Id).SequenceEqual(orderedMatchdayIds))
        {
            return;
        }

        DemoteToDraftIfReady();
        var map = _matchdays.ToDictionary(m => m.Id);
        _matchdays.Clear();
        foreach (var id in orderedMatchdayIds)
        {
            _matchdays.Add(map[id]);
        }
    }

    /// <summary>
    /// Adds a fixture under a round.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created fixture.</returns>
    public Fixture AddFixture(RoundId roundId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var round = _rounds.FirstOrDefault(r => r.Id.Equals(roundId))
            ?? throw new DomainException($"Round '{roundId}' was not found.", StageErrorCodes.RoundNotFound);

        DemoteToDraftIfReady();
        var fixture = new Fixture(FixtureId.New());
        round.AddFixture(fixture);
        Raise(new StageFixtureAdded(Id, fixture.Id, clock));
        return fixture;
    }

    /// <summary>
    /// Adds a fixture under a matchday.
    /// </summary>
    /// <param name="matchdayId">The matchday identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created fixture.</returns>
    public Fixture AddFixture(MatchdayId matchdayId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var matchday = _matchdays.FirstOrDefault(m => m.Id.Equals(matchdayId))
            ?? throw new DomainException(
                $"Matchday '{matchdayId}' was not found.",
                StageErrorCodes.MatchdayNotFound);

        DemoteToDraftIfReady();
        var fixture = new Fixture(FixtureId.New());
        matchday.AddFixture(fixture);
        Raise(new StageFixtureAdded(Id, fixture.Id, clock));
        return fixture;
    }

    /// <summary>
    /// Removes a fixture from the stage.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveFixture(FixtureId fixtureId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        _ = GetFixture(fixtureId);

        DemoteToDraftIfReady();
        if (_rounds.Any(round => round.RemoveFixture(fixtureId)))
        {
            Raise(new StageFixtureRemoved(Id, fixtureId, clock));
            return;
        }

        if (!_matchdays.Any(matchday => matchday.RemoveFixture(fixtureId))) return;
        Raise(new StageFixtureRemoved(Id, fixtureId, clock));
    }

    /// <summary>
    /// Attaches a match identity to a fixture. No-op when already on that fixture.
    /// Application validates that the match belongs to this stage (<c>Match.StageId</c>).
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="matchId">The match identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void AttachMatch(FixtureId fixtureId, MatchId matchId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var fixture = GetFixture(fixtureId);
        if (fixture.Contains(matchId))
        {
            return;
        }

        if (HasMatch(matchId))
        {
            throw new DomainException(
                $"Match '{matchId}' is already attached to another fixture.",
                StageErrorCodes.MatchAlreadyAttached);
        }

        DemoteToDraftIfReady();
        fixture.AttachMatch(matchId);
        Raise(new StageMatchAttached(Id, fixtureId, matchId, clock));
    }

    /// <summary>
    /// Detaches a match identity from a fixture.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="matchId">The match identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void DetachMatch(FixtureId fixtureId, MatchId matchId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var fixture = GetFixture(fixtureId);
        if (!fixture.Contains(matchId))
        {
            throw new DomainException(
                $"Match '{matchId}' is not attached to fixture '{fixtureId}'.",
                StageErrorCodes.MatchNotAttached);
        }

        DemoteToDraftIfReady();
        fixture.DetachMatch(matchId);
        Raise(new StageMatchDetached(Id, fixtureId, matchId, clock));
    }

    /// <summary>
    /// Prepares the stage for its current abstraction level (Draft to Ready).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Prepare(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(StageStatus.Draft, "Stage can only be prepared from Draft.");

        if (!HasStructure)
        {
            throw new DomainException(
                "Stage requires structure before Prepare.",
                StageErrorCodes.NotReady);
        }

        // Elimination (Phase 5.5): ≥1 Round is enough; fixtures optional for Prepare.
        // Championship: Matchdays only. Poules: Groups + Matchdays + ≥1 entry.
        if (_rounds.Count == 0 && _groups.Count > 0)
        {
            if (_matchdays.Count == 0)
            {
                throw new DomainException(
                    "Poule stage requires at least one matchday before Prepare.",
                    StageErrorCodes.InvalidConfiguration);
            }

            if (_groups.All(g => g.EntryIds.Count == 0))
            {
                throw new DomainException(
                    "Poule stage requires at least one group with an entry before Prepare.",
                    StageErrorCodes.InvalidConfiguration);
            }
        }

        Status = StageStatus.Ready;
        Raise(new StagePrepared(Id, clock));
    }

    /// <summary>
    /// Starts the stage (Ready to Running).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Start(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(StageStatus.Ready, "Stage can only be started from Ready.");
        Status = StageStatus.Running;
        Raise(new StageStarted(Id, clock));
    }

    /// <summary>
    /// Suspends a running stage.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Suspend(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(StageStatus.Running, "Stage can only be suspended from Running.");
        Status = StageStatus.Suspended;
        Raise(new StageSuspended(Id, clock));
    }

    /// <summary>
    /// Resumes a suspended stage.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Resume(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(StageStatus.Suspended, "Stage can only be resumed from Suspended.");
        Status = StageStatus.Running;
        Raise(new StageResumed(Id, clock));
    }

    /// <summary>
    /// Completes the stage.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Complete(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status is not (StageStatus.Running or StageStatus.Suspended))
        {
            throw new DomainException(
                $"Stage cannot be completed from '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }

        Status = StageStatus.Completed;
        Raise(new StageCompleted(Id, clock));
    }

    private static void EnsurePermutation<TId>(IReadOnlyList<TId> ordered, IReadOnlyList<TId> current)
        where TId : notnull
    {
        if (ordered.Count != current.Count
            || ordered.Distinct().Count() != ordered.Count
            || ordered.Any(id => !current.Contains(id)))
        {
            throw new DomainException(
                "Order must be a permutation of the current identities.",
                StageErrorCodes.InvalidOrder);
        }
    }

    private void DemoteToDraftIfReady()
    {
        if (Status == StageStatus.Ready)
        {
            Status = StageStatus.Draft;
        }
    }

    private void EnsureDraftOrReady()
    {
        if (Status is not (StageStatus.Draft or StageStatus.Ready))
        {
            throw new DomainException(
                $"Operation is not allowed when status is '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }
    }

    private void EnsureStandingRulesMutable()
    {
        if (Status is StageStatus.Completed)
        {
            throw new DomainException(
                $"Standing rules cannot be modified when status is '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }
    }

    private void EnsureStructureMutable()
    {
        if (Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new DomainException(
                $"Structure cannot be modified when status is '{Status}'.",
                StageErrorCodes.StructureLocked);
        }
    }

    private void EnsureCanAddGroup()
    {
        if (_rounds.Count > 0)
        {
            throw new DomainException(
                "Groups cannot be combined with rounds.",
                StageErrorCodes.InvalidComposition);
        }
    }

    private void EnsureCanAddRound()
    {
        if (_groups.Count > 0)
        {
            throw new DomainException(
                "Rounds cannot be combined with groups.",
                StageErrorCodes.InvalidComposition);
        }

        if (_matchdays.Count > 0)
        {
            throw new DomainException(
                "Rounds cannot be combined with matchdays.",
                StageErrorCodes.InvalidComposition);
        }
    }

    private void EnsureCanAddMatchday()
    {
        if (_rounds.Count > 0)
        {
            throw new DomainException(
                "Matchdays cannot be combined with rounds.",
                StageErrorCodes.InvalidComposition);
        }
    }

    private void EnsureStatus(StageStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainException(message, StageErrorCodes.InvalidTransition);
        }
    }
}
