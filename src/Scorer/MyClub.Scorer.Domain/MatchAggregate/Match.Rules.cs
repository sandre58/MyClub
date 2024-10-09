// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyClub.Domain.Enums;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class MatchRules : ValueObject
    {
        public static readonly MatchRules Default = new([CardColor.Red, CardColor.Yellow]);

        public MatchRules(IEnumerable<CardColor> allowedCards)
        {
            AllowedCards = allowedCards.ToList().AsReadOnly();
        }

        public IReadOnlyCollection<CardColor> AllowedCards { get; }

        public override string ToString()
        {
            var str = new StringBuilder(string.Join("|", AllowedCards));

            return str.ToString();
        }
    }
}
