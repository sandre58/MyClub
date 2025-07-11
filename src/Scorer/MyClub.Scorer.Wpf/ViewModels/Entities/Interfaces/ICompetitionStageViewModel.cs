// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface ICompetitionStageViewModel : IStageViewModel, IAppointment
    {
        ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        Task EditAsync();

        Task OpenAsync();
    }
}
