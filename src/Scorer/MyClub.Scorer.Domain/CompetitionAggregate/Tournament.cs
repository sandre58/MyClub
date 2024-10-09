// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Tournament : AuditableEntity, ICompetition
    {
        private readonly ExtendedObservableCollection<ITournamentStage> _stages = [];

        public Tournament() : this(SchedulingParameters.Default) { }

        public Tournament(SchedulingParameters schedulingParameters)
        {
            Stages = new(_stages);
            SchedulingParameters = schedulingParameters;
        }

        public ReadOnlyObservableCollection<ITournamentStage> Stages { get; }

        public MatchFormat MatchFormat { get; set; } = MatchFormat.NoDraw;

        public MatchRules MatchRules { get; set; } = MatchRules.Default;

        public SchedulingParameters SchedulingParameters { get; set; }

        public IEnumerable<Match> GetAllMatches() => Stages.SelectMany(x => x.GetAllMatches());

        public IEnumerable<T> GetStages<T>() where T : ICompetitionStage => Stages.OfType<T>().Union(Stages.SelectMany(x => x.GetStages<T>()));

        public bool RemoveMatch(Match item) => _stages.Any(x => x.RemoveMatch(item));
    }
}

