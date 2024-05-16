// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.Extensions
{
    internal static class IMatchExtensions
    {
        public static CompetitionViewModel GetCompetition(this IMatchParent parent) => parent.Parent is null ? (parent as CompetitionViewModel)! : parent.Parent is CompetitionViewModel competition ? competition : parent.Parent.GetCompetition();

        public static IEnumerable<MatchViewModel> GetMyMatches(this IMatchParent parent) => parent.AllMatches.Where(x => x.IsMyMatch);

        public static MatchViewModel? GetNextMatch(this IMatchParent parent) => parent.AllMatches.OrderBy(x => x.Date).FirstOrDefault(x => x.Date.IsInFuture());

        public static MatchViewModel? GetPreviousMatch(this IMatchParent parent) => parent.AllMatches.OrderBy(x => x.Date).LastOrDefault(x => x.Date.IsInPast());
    }
}
