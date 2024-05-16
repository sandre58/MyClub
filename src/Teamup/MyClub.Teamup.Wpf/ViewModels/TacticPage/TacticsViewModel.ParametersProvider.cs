// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TacticPage
{
    internal class TacticsListParametersProvider() : ListParametersProvider(nameof(TacticViewModel.Order))
    {
    }
}
