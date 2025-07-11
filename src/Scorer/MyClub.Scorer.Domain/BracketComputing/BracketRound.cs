// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MyClub.Scorer.Domain.BracketComputing
{
    public class BracketRound(ICollection<BracketFixture> fixtures, IDictionary<BracketType, BracketRound> children)
    {
        public ICollection<BracketFixture> Fixtures { get; } = fixtures;

        public IDictionary<BracketType, BracketRound> Children { get; } = children;
    }
}
