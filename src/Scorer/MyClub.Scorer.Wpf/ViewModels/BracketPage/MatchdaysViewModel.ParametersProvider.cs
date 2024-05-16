// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class MatchdaysListParametersProvider : ListParametersProvider
    {
        public override IDisplayViewModel ProvideDisplay()
            => new DisplayViewModel().AddMode<DisplayModeMonth>(true).AddMode<DisplayModeYear>().AddMode<DisplayModeGrid>();
    }
}
