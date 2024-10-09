// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using DynamicData;
using MyClub.Domain.Enums;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.MatchDetails
{
    internal class MatchDetailsViewModel : ItemDialogViewModel<MatchViewModel>
    {
        private readonly ObservableCollection<CardColorWrapper> _homeCards = [];
        private readonly ObservableCollection<CardColorWrapper> _awayCards = [];
        private readonly ObservableCollection<IMatchEventViewModel> _homeEvents = [];
        private readonly ObservableCollection<IMatchEventViewModel> _awayEvents = [];
        private readonly ObservableCollection<TimelineEventsWrapper> _events = [];

        public MatchDetailsViewModel()
        {
            HomeCards = new(_homeCards);
            AwayCards = new(_awayCards);
            HomeEvents = new(_homeEvents);
            AwayEvents = new(_awayEvents);
            Events = new(_events);
        }

        protected override void OnItemChanged()
        {
            base.OnItemChanged();

            if (Item is null) return;

            var homeEvents = setEvents(Item.Home, _homeCards, _homeEvents);
            var awayEvents = setEvents(Item.Away, _awayCards, _awayEvents);
            _events.Set(homeEvents.Union(awayEvents).OrderBy(x => x.Item.Minute));

            IEnumerable<TimelineEventsWrapper> toTimelineEvents(IEnumerable<IMatchEventViewModel> events, Color? color, bool isHome)
                => events.Where(x => x.Minute.HasValue)
                         .GroupBy(x => !Item.AfterExtraTime && !Item.AfterShootouts
                                        ? Math.Min(x.Minute!.Value, (int)Item.Format.RegulationTime.GetFullTime(false).TotalMinutes)
                                        : Math.Min(x.Minute!.Value, (int)Item.Format.GetFullTime(false).TotalMinutes))
                         .SelectMany(x => x.Select((y, z) => new TimelineEventsWrapper(y, x.Key, color, isHome, z))).ToList();

            IEnumerable<TimelineEventsWrapper> setEvents(MatchOpponentViewModel opponent, ObservableCollection<CardColorWrapper> cards, ObservableCollection<IMatchEventViewModel> events)
            {
                cards.Clear();
                events.Clear();
                if (opponent.Team is TeamViewModel team)
                {
                    cards.AddRange(opponent.Cards.GroupBy(x => x.Color).Select(x => new CardColorWrapper(x.Key, x.Select(y => y).OrderBy(x => x.Minute).ToList())).OrderBy(x => x.Item));
                    events.AddRange(opponent.Cards.Union(opponent.Goals.OfType<IMatchEventViewModel>()).OrderBy(x => x.Minute).ToList());

                    return toTimelineEvents(events, team.HomeColor, true);
                }

                return [];
            }
        }

        public ReadOnlyObservableCollection<CardColorWrapper> HomeCards { get; private set; }

        public ReadOnlyObservableCollection<CardColorWrapper> AwayCards { get; private set; }

        public ReadOnlyObservableCollection<IMatchEventViewModel> HomeEvents { get; private set; }

        public ReadOnlyObservableCollection<IMatchEventViewModel> AwayEvents { get; private set; }

        public ReadOnlyObservableCollection<TimelineEventsWrapper> Events { get; private set; }
    }

    internal class CardColorWrapper : Wrapper<CardColor>
    {
        public CardColorWrapper(CardColor item, IEnumerable<CardViewModel> cards) : base(item) => Cards = new ReadOnlyObservableCollection<CardViewModel>(new ObservableCollection<CardViewModel>(cards));

        public ReadOnlyObservableCollection<CardViewModel> Cards { get; }
    }

    internal class TimelineEventsWrapper : Wrapper<IMatchEventViewModel>
    {
        public TimelineEventsWrapper(IMatchEventViewModel item, int minute, Color? color, bool isHome, int index) : base(item)
        {
            Minute = minute;
            IsHome = isHome;
            Index = index;
            Color = color;
        }

        public int Minute { get; }

        public bool IsHome { get; }

        public int Index { get; }

        public Color? Color { get; }

        public string? DisplayName => Item.DisplayName;
    }
}
