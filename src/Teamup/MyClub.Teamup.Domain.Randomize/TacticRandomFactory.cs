// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Helpers;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.Factories.Extensions;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.Randomize
{
    public static class TacticRandomFactory
    {
        public static IEnumerable<Tactic> RandomKnownTactics(int min = 1, int max = 3)
        {
            var tactics = RandomGenerator.ListItems(Enumeration.GetAll<KnownTactic>(), RandomGenerator.Int(min, max));

            return tactics.Select(x => x.CreateTactic());
        }

        public static Tactic Random(string name, int countPositions = 11)
        {
            var tactic = new Tactic(name)
            {
                Description = SentenceGenerator.Paragraph(10, 5),
                Code = RandomGenerator.String2(5)
            };
            EnumerableHelper.Iteration(RandomGenerator.Int(3, 10), _ => tactic.Instructions.Add(SentenceGenerator.Sentence(5, 10)));

            var positions = RandomGenerator.ListItems(Enumeration.GetAll<Position>(), countPositions);

            foreach (var position in positions)
            {
                var tacticPosition = new TacticPosition(position)
                {
                    Number = RandomGenerator.Number(TacticPosition.AcceptableRangeNumber.Min ?? 1, TacticPosition.AcceptableRangeNumber.Max ?? 99),
                    OffsetX = RandomGenerator.Number(-5, 5),
                    OffsetY = RandomGenerator.Number(-5, 5),
                };
                EnumerableHelper.Iteration(RandomGenerator.Int(0, 5), _ => tacticPosition.Instructions.Add(SentenceGenerator.Sentence(5, 10)));

                tactic.AddPosition(tacticPosition);
            }

            return tactic;
        }
    }
}
