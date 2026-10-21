// -----------------------------------------------------------------------
// <copyright file="CompetitionStageRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Persistence row for an ordered stage identity owned by a competition.
/// </summary>
internal sealed class CompetitionStageRef
{
    public CompetitionId CompetitionId { get; set; }

    public StageId StageId { get; set; }

    public int SortOrder { get; set; }
}
