// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;
using MyClub.Teamup.Domain.MatchAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class FriendlyRules : CompetitionRules
    {
        public static readonly FriendlyRules Default = new(MatchFormat.Default, 15.Hours());

        public FriendlyRules(MatchFormat matchFormat, TimeSpan matchTime) : base(matchFormat, matchTime) { }
    }
}
