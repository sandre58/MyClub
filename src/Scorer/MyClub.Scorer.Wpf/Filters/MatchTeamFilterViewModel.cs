// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.ViewModels.List.Filtering.Filters;

namespace MyClub.Scorer.Wpf.Filters
{
    internal class MatchTeamFilterViewModel : SelectedValueFilterViewModel<IVirtualTeamViewModel, IVirtualTeamViewModel>
    {
        public EnumValueFilterViewModel<VenueContext> VenueFilter { get; }

        public bool ShowVenueFilter { get; set; } = true;

        public MatchTeamFilterViewModel(IEnumerable<IVirtualTeamViewModel> teams) : base(string.Empty, teams)
        {
            VenueFilter = new(string.Empty);
            VenueFilter.WhenPropertyChanged(x => x.Value).Subscribe(_ => RaisePropertyChanged(nameof(VenueFilter)));
        }

        protected override FilterViewModel CreateCloneInstance()
        {
            var result = new MatchTeamFilterViewModel(AvailableValues!) { ShowVenueFilter = ShowVenueFilter };
            result.VenueFilter.Value = VenueFilter.Value;

            return result;
        }

        public override void SetFrom(object? from)
        {
            base.SetFrom(from);

            if (from is MatchTeamFilterViewModel other)
            {
                VenueFilter.Value = other.VenueFilter.Value;
            }
        }

        public override void Reset()
        {
            base.Reset();

            VenueFilter.Value = null;
        }

        protected override bool IsMatchProperty(object toCompare)
            => Value is null
               || toCompare is MatchViewModel match
               && (VenueFilter.Value is null || !ShowVenueFilter
                    ? match.Participate(Value)
                    : VenueFilter.Value switch
                    {
                        VenueContext.Home => match.Home.Team == Value,
                        VenueContext.Neutral => match.Participate(Value) && match.NeutralVenue,
                        VenueContext.Away => match.Away.Team == Value,
                        _ => throw new InvalidOperationException("Unknown venue")
                    });

        protected override bool IsMatchPropertyList(IEnumerable<object> toCompareEnumerable)
            => throw new InvalidOperationException("Could ot compare items");
    }
}
