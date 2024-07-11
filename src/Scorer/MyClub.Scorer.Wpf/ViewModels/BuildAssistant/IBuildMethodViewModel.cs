﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Application.Dtos;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal interface IBuildMethodViewModel : IValidatable
    {
        BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeSpan defaultTime);

        void Reset(DateTime startDate);

        void Refresh(DateTime startDate);
    }
}
