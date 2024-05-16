// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities.DateTimes;

namespace MyClub.Domain
{
    public interface IHasPeriod
    {
        ObservablePeriod Period { get; }
    }
}
