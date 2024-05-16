// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using DynamicData.Binding;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Filters
{
    internal class MatchTeamFilterViewModel : SelectedValueFilterViewModel<TeamViewModel, TeamViewModel>
    {
        public EnumValueFilterViewModel<Venue> VenueFilter { get; }

        public bool ShowVenueFilter { get; set; } = true;

        public MatchTeamFilterViewModel() : base(string.Empty, [])
        {
            VenueFilter = new(string.Empty);
            VenueFilter.WhenPropertyChanged(x => x.Value).Subscribe(_ => RaisePropertyChanged(nameof(VenueFilter)));
        }

        protected override FilterViewModel CreateCloneInstance()
        {
            var result = new MatchTeamFilterViewModel() { ShowVenueFilter = ShowVenueFilter };
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
               && (VenueFilter.Value is null
                    ? match.Participate(Value)
                    : VenueFilter.Value switch
                    {
                        Venue.Home => match.HomeTeam == Value,
                        Venue.Neutral => match.Participate(Value) && match.NeutralVenue,
                        Venue.Away => match.AwayTeam == Value,
                        _ => throw new InvalidOperationException("Unknown venue")
                    });

        protected override bool IsMatchPropertyList(IEnumerable<object> toCompareEnumerable)
            => throw new InvalidOperationException("Could ot compare items");

        public void Initialize(IEnumerable<TeamViewModel> allTeams)
        {
            Value = null;
            AvailableValues = allTeams;
        }
    }
}
