// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;

namespace MyClub.Teamup.Domain.ProjectAggregate
{
    public class ProjectPreferences : ValueObject
    {
        public ProjectPreferences(TimeSpan trainingStartTime, TimeSpan trainingDuration)
        {
            TrainingStartTime = trainingStartTime;
            TrainingDuration = trainingDuration;
        }

        public TimeSpan TrainingStartTime { get; }

        public TimeSpan TrainingDuration { get; }
    }
}
