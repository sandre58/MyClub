// -----------------------------------------------------------------------
// <copyright file="MatchFilter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Standings;

/// <summary>
/// Which matches contribute to a standing calculation (orthogonal to <see cref="Rules.RankingScope"/>).
/// </summary>
public enum MatchFilter
{
    /// <summary>
    /// All matches involving participants.
    /// </summary>
    All = 0,

    /// <summary>
    /// Only count a match for an entry when that entry is home.
    /// </summary>
    Home = 1,

    /// <summary>
    /// Only count a match for an entry when that entry is away.
    /// </summary>
    Away = 2
}
