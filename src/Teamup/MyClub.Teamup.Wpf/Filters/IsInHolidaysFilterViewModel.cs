// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.Filters
{
    internal class IsInHolidaysFilterViewModel : BooleanFilterViewModel
    {
        private readonly HolidaysProvider _holidaysProvider;

        public IsInHolidaysFilterViewModel(string property, HolidaysProvider holidaysProvider) : base(property)
        {
            AllowNullValue = true;
            _holidaysProvider = holidaysProvider;
        }

        protected override bool IsMatchProperty(object toCompare) => toCompare is DateTime date && Value.HasValue && _holidaysProvider.Items.Any(x => x.ContainsDate(date)) == Value;

        protected override FilterViewModel CreateCloneInstance() => new IsInHolidaysFilterViewModel(PropertyName, _holidaysProvider);
    }
}
