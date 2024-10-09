// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class VenueRulesMatchesScheduler : IMatchesScheduler
    {
        private readonly IEnumerable<Match> _scheduledMatches;

        private readonly IEnumerable<Stadium> _stadiums;

        public VenueRulesMatchesScheduler(IEnumerable<Stadium> stadiums, IEnumerable<Match>? scheduledMatches = null)
        {
            _stadiums = stadiums;
            _scheduledMatches = scheduledMatches ?? [];
        }

        public List<IAvailableVenueSchedulingRule> Rules { get; set; } = [];

        public void Schedule(IEnumerable<Match> matches)
        {
            var newScheduledMatches = new List<Match>(_scheduledMatches.Except(matches));
            matches.ForEach(match =>
            {
                var result = Rules.Select(x => x.GetAvailableStadium(match, newScheduledMatches, _stadiums)).FirstOrDefault(x => x is not null);

                if (result.HasValue)
                {
                    match.Stadium = result.Value.StadiumId.HasValue ? _stadiums.GetById(result.Value.StadiumId.Value) : null;
                    match.IsNeutralStadium = result.Value.IsNeutral;
                }
                newScheduledMatches.Add(match);
            });
        }
    }
}

