// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface ISchedulingParametersProvider
    {
        SchedulingParameters ProvideSchedulingParameters();
    }
}
