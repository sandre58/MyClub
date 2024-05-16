// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using DynamicData.Binding;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.Edition;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant
{
    internal class SchedulingAssistantViewModel : EditionViewModel
    {
        private TimeSpan? _time;
        private DateTime? _date;

        public SchedulingAssistantViewModel(IEnumerable<SchedulingSerie> series, DateTime? date)
        {
            Series = series.ToList().AsReadOnly();
            Appointments = new(Series.Select(x => x.Select(y => new SchedulingAppointment(y, x.Color))).SelectMany(x => x).ToList());

            if (date.HasValue)
            {
                DateSelection.DisplayDate = date.Value.ToLocalTime().Date;
                DateTimeSelection.DisplayDate = date.Value.ToLocalTime().BeginningOfHour();
            }
            FullDate = date;

            IncrementTimeCommand = CommandsManager.Create(() => Time = Time?.Add(1.Minutes()) ?? TimeSpan.Zero, () => Time < new TimeSpan(23, 59, 0));
            DecrementTimeCommand = CommandsManager.Create(() => Time = Time?.Subtract(1.Minutes()), () => Time.HasValue && Time > TimeSpan.Zero);

            Disposables.AddRange(
            [
                DateSelection.WhenPropertyChanged(x => x.DisplayDate, false).Subscribe(_ => DateTimeSelection.DisplayDate = DateSelection.DisplayDate.SetTime(DateTimeSelection.DisplayDate.TimeOfDay)),
                DateTimeSelection.WhenPropertyChanged(x => x.DisplayDate, false).Subscribe(_ => DateSelection.DisplayDate = DateTimeSelection.DisplayDate.BeginningOfDay()),
            ]);
        }

        public DisplayModeMonth DateSelection { get; } = new();

        public DisplayModeDay DateTimeSelection { get; } = new(4, 8.Hours(), 23.Hours());

        public DateTime? FullDate
        {
            get => _date?.ToLocalDateTime(_time ?? TimeSpan.Zero);
            set
            {
                if (FullDate != value)
                {
                    Date = value?.ToLocalTime().Date;
                    Time = value?.ToLocalTime().TimeOfDay;
                }
            }
        }

        public DateTime? Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value?.Date;
                    RaisePropertyChanged(nameof(Date));
                    RaisePropertyChanged(nameof(FullDate));

                    if (_date.HasValue)
                        DateTimeSelection.DisplayDate = _date.Value.SetTime(DateTimeSelection.DisplayDate.TimeOfDay);
                }
            }
        }

        public TimeSpan? Time
        {
            get => _time;
            set
            {
                if (_time != value)
                {
                    _time = value;
                    RaisePropertyChanged(nameof(Time));
                    RaisePropertyChanged(nameof(FullDate));
                }
            }
        }

        public ReadOnlyCollection<SchedulingSerie> Series { get; }

        public ListViewModel<SchedulingAppointment> Appointments { get; }

        public ICommand IncrementTimeCommand { get; }

        public ICommand DecrementTimeCommand { get; }

        protected override bool CanSave() => FullDate.HasValue;

        protected override void SaveCore() { }
    }

    internal class SchedulingAppointment : Wrapper<IAppointment>, IAppointment
    {
        public SchedulingAppointment(IAppointment appointment, Color? color = null) : base(appointment) => Color = color;

        public Color? Color { get; set; }

        public DateTime StartDate => Item.StartDate;

        public DateTime EndDate => Item.EndDate;

        public override string ToString() => Item.ToString().OrEmpty();
    }

    internal class SchedulingSerie : List<IAppointment>
    {
        public SchedulingSerie(IEnumerable<IAppointment> appointments, object legend, Color? color = null) : base(appointments)
        {
            Legend = legend;
            Color = color ?? RandomGenerator.Color().ToColor();
        }

        public object Legend { get; set; }

        public Color? Color { get; set; }

        public override string ToString() => Legend.ToString().OrEmpty();
    }
}
