// -----------------------------------------------------------------------
// <copyright file="SlotFeedResolverTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages.Feed;

public sealed class SlotFeedResolverTests
{
    private readonly StageId _target = StageId.New();
    private readonly StageId _source = StageId.New();

    [Fact]
    public void Direct_assignment_resolves_unique_direct()
    {
        var entryId = EntryId.New();
        var snapshot = Snapshot(
            ["SF1-A"],
            directs: [new DirectFeedSource("SF1-A", entryId)]);

        var result = SlotFeedResolver.Resolve(snapshot, "SF1-A");

        result.Status.Should().Be(FeedResolutionStatus.Unique);
        result.Source!.Kind.Should().Be(FeedKind.Direct);
        result.Source.Direct!.ConfiguredEntryId.Should().Be(entryId);
        result.ContributingKinds.Should().Equal(FeedKind.Direct);
    }

    [Fact]
    public void Slot_without_configuration_is_missing_even_if_entry_would_exist()
    {
        // Snapshot never carries Slot.EntryId — empty config ⇒ Missing.
        var snapshot = Snapshot(["SF1-A"]);

        var result = SlotFeedResolver.Resolve(snapshot, "SF1-A");

        result.Status.Should().Be(FeedResolutionStatus.Missing);
        result.Source.Should().BeNull();
        result.ContributingKinds.Should().BeEmpty();
    }

    [Fact]
    public void Direct_plus_progression_is_multiple_feeds()
    {
        var snapshot = Snapshot(
            ["SF1-A"],
            directs: [new DirectFeedSource("SF1-A", EntryId.New())],
            progressions:
            [
                new ProgressionFeedSource(_source, FixtureId.New().Value.ToString("N"), ProgressionOutcome.Winner, "SF1-A")
            ]);

        var result = SlotFeedResolver.Resolve(snapshot, "SF1-A");

        result.Status.Should().Be(FeedResolutionStatus.MultipleFeeds);
        result.ContributingKinds.Should().BeEquivalentTo([FeedKind.Direct, FeedKind.Progression]);
    }

    [Fact]
    public void Progression_winner_resolves_unique()
    {
        var fixtureId = FixtureId.New();
        var snapshot = Snapshot(
            ["SF1-A"],
            progressions:
            [
                new ProgressionFeedSource(_source, fixtureId.Value.ToString("N"), ProgressionOutcome.Winner, "SF1-A")
            ]);

        var result = SlotFeedResolver.Resolve(snapshot, "SF1-A");

        result.Status.Should().Be(FeedResolutionStatus.Unique);
        result.Source!.Progression!.Outcome.Should().Be(ProgressionOutcome.Winner);
        result.Source.Progression.SourcePairKey.Should().Be(fixtureId.Value.ToString("N"));
        result.Source.Progression.SourceStageId.Should().Be(_source);
    }

    [Fact]
    public void Progression_loser_resolves_unique()
    {
        var snapshot = Snapshot(
            ["Consolante"],
            progressions:
            [
                new ProgressionFeedSource(_source, FixtureId.New().Value.ToString("N"), ProgressionOutcome.Loser, "Consolante")
            ]);

        var result = SlotFeedResolver.Resolve(snapshot, "Consolante");

        result.Status.Should().Be(FeedResolutionStatus.Unique);
        result.Source!.Kind.Should().Be(FeedKind.Progression);
        result.Source.Progression!.Outcome.Should().Be(ProgressionOutcome.Loser);
    }

    [Fact]
    public void Two_progression_paths_are_invalid_feed()
    {
        var snapshot = Snapshot(
            ["SF1-A"],
            progressions:
            [
                new ProgressionFeedSource(_source, FixtureId.New().Value.ToString("N"), ProgressionOutcome.Winner, "SF1-A"),
                new ProgressionFeedSource(_source, FixtureId.New().Value.ToString("N"), ProgressionOutcome.Loser, "SF1-A")
            ]);

        var result = SlotFeedResolver.Resolve(snapshot, "SF1-A");

        result.Status.Should().Be(FeedResolutionStatus.InvalidFeed);
    }

    [Fact]
    public void Progression_plus_draw_is_multiple_feeds()
    {
        var snapshot = Snapshot(
            ["SF1-A"],
            progressions:
            [
                new ProgressionFeedSource(_source, FixtureId.New().Value.ToString("N"), ProgressionOutcome.Winner, "SF1-A")
            ],
            draws: [new DrawFeedSource("SF1-A", DrawId.New())]);

        SlotFeedResolver.Resolve(snapshot, "SF1-A").Status.Should().Be(FeedResolutionStatus.MultipleFeeds);
    }

    [Fact]
    public void Direct_plus_draw_is_multiple_feeds()
    {
        var snapshot = Snapshot(
            ["SF1-A"],
            directs: [new DirectFeedSource("SF1-A", EntryId.New())],
            draws: [new DrawFeedSource("SF1-A", DrawId.New())]);

        SlotFeedResolver.Resolve(snapshot, "SF1-A").Status.Should().Be(FeedResolutionStatus.MultipleFeeds);
    }

    [Fact]
    public void InvalidFeed_precedes_MultipleFeeds_when_same_kind_duplicated()
    {
        var snapshot = Snapshot(
            ["SF1-A"],
            progressions:
            [
                new ProgressionFeedSource(_source, FixtureId.New().Value.ToString("N"), ProgressionOutcome.Winner, "SF1-A"),
                new ProgressionFeedSource(_source, FixtureId.New().Value.ToString("N"), ProgressionOutcome.Loser, "SF1-A")
            ],
            draws: [new DrawFeedSource("SF1-A", DrawId.New())]);

        var result = SlotFeedResolver.Resolve(snapshot, "SF1-A");

        result.Status.Should().Be(FeedResolutionStatus.InvalidFeed);
        result.ContributingKinds.Should().BeEquivalentTo([FeedKind.Progression, FeedKind.Draw]);
    }

    [Fact]
    public void ResolveAll_returns_missing_for_empty_slot()
    {
        var snapshot = Snapshot(["A", "B"]);

        var results = SlotFeedResolver.ResolveAll(snapshot);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Status == FeedResolutionStatus.Missing);
    }

    [Fact]
    public void Unique_draw_resolves_with_draw_id()
    {
        var drawId = DrawId.New();
        var snapshot = Snapshot(["SF1-A"], draws: [new DrawFeedSource("SF1-A", drawId)]);

        var result = SlotFeedResolver.Resolve(snapshot, "SF1-A");

        result.Status.Should().Be(FeedResolutionStatus.Unique);
        result.Source!.Kind.Should().Be(FeedKind.Draw);
        result.Source.Draw!.DrawId.Should().Be(drawId);
    }

    [Fact]
    public void Snapshot_rejects_dangling_destination()
    {
        var act = () => new SlotFeedSnapshot(
            _target,
            ["SF1-A"],
            [],
            [],
            [new ProgressionFeedSource(_source, FixtureId.New().Value.ToString("N"), ProgressionOutcome.Winner, "Missing")],
            []);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FeedSnapshotInvalid);
    }

    private SlotFeedSnapshot Snapshot(
        string[] slotKeys,
        IReadOnlyList<DirectFeedSource>? directs = null,
        IReadOnlyList<ProgressionFeedSource>? progressions = null,
        IReadOnlyList<DrawFeedSource>? draws = null) =>
        new(
            _target,
            slotKeys,
            directs ?? [],
            [],
            progressions ?? [],
            draws ?? []);
}
