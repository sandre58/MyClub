// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public class TournamentProject : Project<Tournament>
    {
        private readonly Dictionary<Guid, SchedulingParameters> _schedulingParametersByStage = [];

        public TournamentProject(string name, byte[]? image = null, SchedulingParameters? schedulingParameters = null, Guid? id = null)
            : base(CompetitionType.Tournament, name, image, id) => DefaultSchedulingParameters = schedulingParameters ?? SchedulingParameters.Default;

        public SchedulingParameters DefaultSchedulingParameters { get; set; }

        public SchedulingParameters GetSchedulingParameters(Guid stageId) => _schedulingParametersByStage.GetValueOrDefault(stageId, DefaultSchedulingParameters);

        public SchedulingParameters SetSchedulingParameters(Guid stageId, SchedulingParameters schedulingParameters) => _schedulingParametersByStage.AddOrUpdate(stageId, schedulingParameters);

        public void ResetSchedulingParameters(Guid stageId) => _schedulingParametersByStage.TryRemove(stageId);
    }
}
