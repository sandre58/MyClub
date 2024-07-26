// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Observable;
using MyNet.UI.Threading;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.StatisticsTab
{
    internal class InjuriesStatisticsInjuriesViewModel : ListViewModel<InjuriesByTypeWrapper>
    {
        public InjuriesStatisticsInjuriesViewModel(ReadOnlyObservableCollection<InjuryViewModel> injuries)
            : base(source: Enum.GetValues<InjuryType>().Select(x => new InjuriesByTypeWrapper(x, injuries)).ToList()) { }
    }

    internal class InjuriesByTypeWrapper : Wrapper<InjuryType>
    {
        private readonly ReadOnlyObservableCollection<InjuryViewModel> _injuries;

        public ReadOnlyObservableCollection<InjuryViewModel> Injuries => _injuries;

        public InjurySeverity? Severity { get; private set; }

        public InjuriesByTypeWrapper(InjuryType item, ReadOnlyObservableCollection<InjuryViewModel> injuries) : base(item)
        {
            var obs = injuries.ToObservableChangeSet(x => x.Id);
            Disposables.AddRange([
                obs.AutoRefresh(x => x.Type).Filter(x => x.Type == item).ObserveOn(Scheduler.UI).SortAndBind(out _injuries, SortExpressionComparer<InjuryViewModel>.Descending(x =>x.Date)).Subscribe(),
                obs.AutoRefresh(x => x.Severity).Subscribe(_ => Severity = Injuries.Any() ? Injuries.Max(x => x.Severity) : null)
                ]);
        }
    }
}
