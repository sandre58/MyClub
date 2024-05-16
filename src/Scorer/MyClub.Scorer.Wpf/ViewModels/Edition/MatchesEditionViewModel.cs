// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Humanizer;
using MyNet.Observable.Attributes;
using MyNet.Observable.Threading;
using MyNet.UI.Resources;
using MyNet.UI.Toasting;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Units;
using MyNet.Wpf.Presentation.Models;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    public enum EditionDateFormula
    {
        Date,

        Offset
    }

    internal class MatchesEditionViewModel : EditionViewModel
    {
        private readonly MatchService _matchService;
        private readonly ReadOnlyObservableCollection<StadiumWrapper> _stadiums;
        private readonly AvailibilityCheckingService _availibilityCheckingService;

        public MatchesEditionViewModel(MatchService matchService,
                                       StadiumsProvider stadiumsProvider,
                                       AvailibilityCheckingService availibilityCheckingService)
        {
            _matchService = matchService;
            _availibilityCheckingService = availibilityCheckingService;

            Mode = ScreenMode.Edition;

            Disposables.AddRange(
                [
                    stadiumsProvider.ConnectById().Transform(x => new StadiumWrapper(x)).Bind(out _stadiums).ObserveOn(Scheduler.UI).Subscribe(),
                    this.WhenPropertyChanged(x => x.DateFormula).Subscribe(_ => ValidateStadiumsAvaibility()),
                    this.WhenPropertyChanged(x => x.OffsetUnit).Subscribe(_ => ValidateStadiumsAvaibility()),
                    Date.WhenAnyPropertyChanged().Subscribe(_ => ValidateStadiumsAvaibility()),
                    Time.WhenAnyPropertyChanged().Subscribe(_ => ValidateStadiumsAvaibility()),
                    Offset.WhenAnyPropertyChanged().Subscribe(_ => ValidateStadiumsAvaibility()),
                ]);
        }

        public IMatchParent? Parent { get; private set; }

        [CanSetIsModified(false)]
        public EditionDateFormula DateFormula { get; private set; }

        public MultipleEditableValue<int> Offset { get; set; } = new();

        public TimeUnit OffsetUnit { get; set; }

        public MultipleEditableValue<DateTime?> Date { get; set; } = new();

        public MultipleEditableValue<TimeSpan?> Time { get; set; } = new();

        public MultipleEditableValue<StadiumWrapper?> Stadium { get; } = new();

        public bool NeutralVenue { get; set; }

        [CanSetIsModified(false)]
        public IReadOnlyCollection<MatchViewModel> Matches { get; private set; } = new List<MatchViewModel>().AsReadOnly();

        public ReadOnlyObservableCollection<StadiumWrapper> Stadiums => _stadiums;

        private void ValidateStadiumsAvaibility()
        {
            if (IsModifiedSuspender.IsSuspended) return;
            foreach (var item in _stadiums)
            {
                var avaibility = AvailabilityCheck.Unknown;
                foreach (var match in Matches)
                {
                    var date = match.Date;

                    if (DateFormula == EditionDateFormula.Date && Date.IsActive)
                    {
                        date = Date.GetActiveValue().GetValueOrDefault().Date.AddFluentTimeSpan(Time.GetActiveValue().GetValueOrDefault());
                    }
                    else if (DateFormula == EditionDateFormula.Offset && Offset.IsActive)
                    {
                        date = date.AddFluentTimeSpan(Offset.GetActiveValue().Unit(OffsetUnit));
                    }
                    var tempAvaibility = CheckStadiumAvaibility(item.Item.Id, date, match);

                    if (tempAvaibility > avaibility)
                        avaibility = tempAvaibility;
                }
                item.Availability = avaibility;
            }
        }

        private AvailabilityCheck CheckStadiumAvaibility(Guid stadiumId, DateTime date, MatchViewModel match)
            => _availibilityCheckingService.GetStadiumAvaibility(stadiumId, date, match.Format, [match.Id]);

        public void Load(IEnumerable<MatchViewModel> matches, EditionDateFormula dateFormula = EditionDateFormula.Date)
        {
            Parent = matches.FirstOrDefault()?.Parent;
            Matches = matches.ToList().AsReadOnly();
            DateFormula = dateFormula;
            Reset();
        }

        protected override void ResetCore()
        {
            base.ResetCore();

            Date.Reset(Matches.Select(x => (DateTime?)x.DateOfDay));
            Time.Reset(Matches.Select(x => (TimeSpan?)x.Date.TimeOfDay));
            Stadium.Reset(Matches.Select(x => _stadiums.FirstOrDefault(y => x.Stadium is not null && y.Item.Id == x.Stadium.Id)));
            NeutralVenue = Matches.All(x => x.NeutralVenue);
            Offset.Reset(5);
            OffsetUnit = TimeUnit.Minute;
            Offset.IsActive = DateFormula == EditionDateFormula.Offset;

            ValidateStadiumsAvaibility();
        }

        protected override void SaveCore()
        {
            if ((!Date.IsActive || !Date.Value.HasValue) && (!Time.IsActive || !Time.Value.HasValue) && !Offset.IsActive && !Stadium.IsActive) return;

            var result = _matchService.Update(Matches.Select(x => x.Id).ToList(), new MatchMultipleDto
            {
                ParentId = Parent?.Id ?? throw new InvalidOperationException("Parent cannot be null"),
                UpdateDate = Date.IsActive && DateFormula == EditionDateFormula.Date,
                UpdateTime = Time.IsActive && DateFormula == EditionDateFormula.Date,
                UpdateStadium = Stadium.IsActive,
                Date = Date.GetActiveValue().GetValueOrDefault().Date,
                Time = Time.GetActiveValue().GetValueOrDefault(),
                NeutralVenue = NeutralVenue,
                OffsetUnit = OffsetUnit,
                Offset = DateFormula == EditionDateFormula.Offset ? Offset.GetActiveValue() : 0,
                Stadium = Stadium.GetActiveValue() is not null ? new StadiumDto
                {
                    Id = Stadium.GetActiveValue()?.Item.Id
                } : null
            });

            ToasterManager.ShowSuccess(nameof(MessageResources.XItemsHasBeenModifiedSuccess).TranslateWithCountAndOptionalFormat(result.Count));
        }
    }
}
