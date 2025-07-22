// -----------------------------------------------------------------------
// <copyright file="VirtualTeamReference.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

public record VirtualTeamReference : TeamReference;

public record FixtureResultReference(RoundId RoundId, FixtureId FixtureId, VirtualTeamType Type) : VirtualTeamReference;

public record GroupRankReference(StageId StageId, GroupId GroupId, int Rank) : VirtualTeamReference;

public record ChampionshipRankReference(StageId StageId, int Rank) : VirtualTeamReference;
