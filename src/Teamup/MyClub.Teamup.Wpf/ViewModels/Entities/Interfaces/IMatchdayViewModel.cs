// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.UI.Commands;
using MyNet.Observable;

namespace MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IMatchdayViewModel : IMatchParent, IAppointment
    {
        bool IsPostponed { get; }

        DateTime Date { get; }

        DateTime OriginDate { get; }

        public ICommand OpenCommand { get; }

        public ICommand AddMatchesCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand EditResultsCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand PostponeCommand { get; }

    }
}
