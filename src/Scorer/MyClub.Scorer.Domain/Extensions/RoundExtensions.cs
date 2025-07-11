// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class RoundExtensions
    {
        public static RoundStage? GetPreviousStage(this RoundStage stage)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);
            var previousIndex = stageIndex - 1;
            return previousIndex >= 0 && previousIndex < stage.Stage.Stages.Count ? stage.Stage.Stages[previousIndex] : null;
        }

        public static RoundStage? GetNextStage(this RoundStage stage)
        {
            var stageIndex = stage.Stage.Stages.IndexOf(stage);
            var nextIndex = stageIndex + 1;
            return nextIndex >= 0 && nextIndex < stage.Stage.Stages.Count ? stage.Stage.Stages[nextIndex] : null;
        }

        public static int GetConsolationLevel(this Round round)
        {
            var rounds = round.GetAncestors().Reverse().Union([round]);
            var isConsolationArray = rounds.Select(r => !r.IsWinnerRound).ToArray();

            var result = 0;
            for (var i = 0; i < isConsolationArray.Length; i++)
            {
                if (isConsolationArray[i])
                    result |= 1 << (isConsolationArray.Length - 1 - i);
            }
            return result;
        }

        public static IEnumerable<Round> GetAncestors(this Round round)
        {
            var ancestor = round.Ancestor;
            while (ancestor is not null)
            {
                yield return ancestor;
                ancestor = ancestor.Ancestor;
            }
        }
    }
}
