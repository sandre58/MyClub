// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal interface IDatesSchedulingMethodViewModel : IValidatable
    {
        BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeOnly defaultTime);

        void Reset(DateTime startDate);

        void Load(SchedulingParameters schedulingParameters);
    }
}
