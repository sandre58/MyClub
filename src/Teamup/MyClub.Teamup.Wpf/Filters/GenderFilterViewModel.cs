// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.Filters
{
    internal class GenderFilterViewModel : EnumValueFilterViewModel<GenderType>
    {
        public GenderFilterViewModel(string propertyName) : base(propertyName) { }
    }
}
