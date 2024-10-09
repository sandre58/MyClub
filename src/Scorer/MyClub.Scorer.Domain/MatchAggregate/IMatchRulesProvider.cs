// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public interface IMatchRulesProvider
    {
        MatchRules ProvideRules();
    }
}
