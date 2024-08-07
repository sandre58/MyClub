﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyNet.Utilities.Extensions;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.Collections;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class KnockoutStage : Knockout, IStage
    {
        private string _name = string.Empty;
        private readonly ExtendedObservableCollection<ITeam> _teams = [];

        public KnockoutStage(string name, IStage? parent = null, SchedulingParameters? schedulingParameters = null, Guid? id = null) : base(id)
        {
            Parent = parent;
            Name = name;
            Teams = new(_teams);
            SchedulingParameters = schedulingParameters ?? SchedulingParameters.Default;
        }

        public string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public ReadOnlyObservableCollection<ITeam> Teams { get; }

        public IStage? Parent { get; }

        public SchedulingParameters SchedulingParameters { get; set; }

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => SchedulingParameters;
    }
}
