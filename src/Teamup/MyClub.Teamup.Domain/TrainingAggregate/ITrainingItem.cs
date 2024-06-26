﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Domain.TrainingAggregate
{
    public interface ITrainingItem
    {
        string Label { get; }

        TimeSpan? Duration { get; }
    }
}
