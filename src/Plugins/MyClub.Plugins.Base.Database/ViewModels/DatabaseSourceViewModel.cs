// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MyClub.DatabaseContext.Domain;
using MyClub.Plugins.Base.Database.Resources;
using MyNet.Observable;
using MyNet.UI.Busy;
using MyNet.UI.Commands;
using MyNet.UI.Extensions;
using MyNet.Utilities;
using MyNet.Utilities.Localization;

namespace MyClub.Plugins.Base.Database.ViewModels
{
    public class DatabaseSourceViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;

        public event EventHandler? ItemsLoadingRequested;

        public DatabaseSourceViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            ConnectionBusyService = BusyManager.Create();
            TestConnectionCommand = CommandsManager.Create(async () => await TestConnectionAsync().ConfigureAwait(false));
            ImportCommand = CommandsManager.Create(AskLoadingItems, () => IsEnabled);

            (Name, Host) = _unitOfWork.GetConnectionInfo();

            TranslationService.RegisterResources(nameof(DatabaseResources), DatabaseResources.ResourceManager);
        }

        public IBusyService ConnectionBusyService { get; }

        public string? Name { get; }

        public string? Host { get; }

        public bool? CanConnect { get; private set; } = null;

        public bool IsEnabled => !CanConnect.IsFalse();

        public ICommand TestConnectionCommand { get; set; }

        public ICommand ImportCommand { get; set; }

        public void AskLoadingItems() => ItemsLoadingRequested?.Invoke(this, EventArgs.Empty);

        public async Task InitializeAsync() => await TestConnectionAsync().ConfigureAwait(false);

        private async Task TestConnectionAsync()
            => await ConnectionBusyService.WaitIndeterminateAsync(() =>
            {
                CanConnect = _unitOfWork.CanConnect();
                RaisePropertyChanged(nameof(IsEnabled));
            }).ConfigureAwait(false);
    }
}
