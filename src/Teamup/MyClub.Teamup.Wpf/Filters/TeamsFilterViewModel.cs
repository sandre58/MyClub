// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;
using MyClub.Teamup.Wpf.Collections;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Filters
{
    internal class TeamsFilterViewModel : SelectedValuesFilterViewModel<TeamViewModel>
    {
        public bool ExternalPlayers { get; set; }

        public bool ShowExternalPlayers { get; set; } = true;

        public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.Or;

        public bool ShowLogicalOperator { get; set; }

        public TeamsFilterViewModel(string propertyName, string selectedValuePath = nameof(TeamViewModel.Id))
            : base(propertyName, TeamsCollection.MyTeams, selectedValuePath) { }

        public override bool IsEmpty() => Values == null;

        protected override FilterViewModel CreateCloneInstance() => new TeamsFilterViewModel(PropertyName, SelectedValuePath) { ShowExternalPlayers = ShowExternalPlayers, ShowLogicalOperator = ShowLogicalOperator };

        public override void SetFrom(object? from)
        {
            base.SetFrom(from);

            if (from is TeamsFilterViewModel other)
            {
                ExternalPlayers = other.ExternalPlayers;
                LogicalOperator = other.LogicalOperator;
            }
        }

        public override void Reset()
        {
            base.Reset();

            ExternalPlayers = false;
        }

        protected override bool IsMatchProperty(object toCompare)
        {
            var res = base.IsMatchProperty(toCompare);

            if (ExternalPlayers)
                res = res || toCompare is null;

            return res;
        }

        protected override bool IsMatchPropertyList(IEnumerable<object> toCompareEnumerable)
            => LogicalOperator switch
            {
                LogicalOperator.And => toCompareEnumerable.All(IsMatchProperty),
                _ => base.IsMatchPropertyList(toCompareEnumerable),
            };

    }
}
