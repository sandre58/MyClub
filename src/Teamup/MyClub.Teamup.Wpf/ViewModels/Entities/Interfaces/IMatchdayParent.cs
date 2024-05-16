// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IMatchdayParent : IMatchParent
    {
        IEnumerable<IMatchdayViewModel> GetAllMatchdays();
    }
}
