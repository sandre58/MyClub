// -----------------------------------------------------------------------
// <copyright file="UniqueFeedSource.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Structured unique feed source (read model VO — not persisted, not an entity).
/// </summary>
public sealed record UniqueFeedSource
{
    private UniqueFeedSource(
        FeedKind kind,
        QualificationFeedRef? qualification,
        ProgressionFeedRef? progression,
        DirectFeedRef? direct,
        DrawFeedRef? draw)
    {
        Kind = kind;
        Qualification = qualification;
        Progression = progression;
        Direct = direct;
        Draw = draw;
    }

    /// <summary>
    /// Gets the feed kind.
    /// </summary>
    public FeedKind Kind { get; }

    /// <summary>
    /// Gets the qualification ref when <see cref="Kind"/> is <see cref="FeedKind.Qualification"/>.
    /// </summary>
    public QualificationFeedRef? Qualification { get; }

    /// <summary>
    /// Gets the progression ref when <see cref="Kind"/> is <see cref="FeedKind.Progression"/>.
    /// </summary>
    public ProgressionFeedRef? Progression { get; }

    /// <summary>
    /// Gets the direct ref when <see cref="Kind"/> is <see cref="FeedKind.Direct"/>.
    /// </summary>
    public DirectFeedRef? Direct { get; }

    /// <summary>
    /// Gets the draw ref when <see cref="Kind"/> is <see cref="FeedKind.Draw"/>.
    /// </summary>
    public DrawFeedRef? Draw { get; }

    /// <summary>
    /// Creates a unique qualification feed source.
    /// </summary>
    public static UniqueFeedSource ForQualification(StageId sourceStageId, int pathOrder) =>
        new(FeedKind.Qualification, new QualificationFeedRef(sourceStageId, pathOrder), null, null, null);

    /// <summary>
    /// Creates a unique progression feed source.
    /// </summary>
    public static UniqueFeedSource ForProgression(
        StageId sourceStageId,
        FixtureId sourceFixtureId,
        ProgressionOutcome outcome) =>
        new(FeedKind.Progression, null, new ProgressionFeedRef(sourceStageId, sourceFixtureId, outcome), null, null);

    /// <summary>
    /// Creates a unique direct feed source from configuration.
    /// </summary>
    public static UniqueFeedSource ForDirect(EntryId configuredEntryId) =>
        new(FeedKind.Direct, null, null, new DirectFeedRef(configuredEntryId), null);

    /// <summary>
    /// Creates a unique draw feed source placeholder.
    /// </summary>
    public static UniqueFeedSource ForDraw() =>
        new(FeedKind.Draw, null, null, null, new DrawFeedRef());
}
