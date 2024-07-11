// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class CupProject : Project<Cup>
    {
        private readonly Dictionary<Guid, SchedulingParameters> _schedulingParametersByRound = [];

        public CupProject(string name, byte[]? image = null, SchedulingParameters? schedulingParameters = null, Guid? id = null)
            : base(CompetitionType.Cup, name, image, id) => DefaultSchedulingParameters = schedulingParameters ?? SchedulingParameters.Default;

        public SchedulingParameters DefaultSchedulingParameters { get; set; }

        public SchedulingParameters GetSchedulingParameters(Guid roundId) => _schedulingParametersByRound.GetValueOrDefault(roundId, DefaultSchedulingParameters);

        public SchedulingParameters SetSchedulingParameters(Guid roundId, SchedulingParameters schedulingParameters) => _schedulingParametersByRound.AddOrUpdate(roundId, schedulingParameters);

        public void ResetSchedulingParameters(Guid roundId) => _schedulingParametersByRound.TryRemove(roundId);
    }
}
