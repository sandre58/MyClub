// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Observable;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class RoundDetailsViewModel : Wrapper<RoundViewModel>
    {
        public RoundDetailsViewModel(RoundViewModel item) : base(item)
        {
        }
    }
}
