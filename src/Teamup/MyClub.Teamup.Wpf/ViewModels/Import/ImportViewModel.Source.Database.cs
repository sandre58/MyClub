// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MyNet.UI.Busy;
using MyNet.UI.Commands;
using MyNet.UI.Extensions;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class ImportSourceDatabaseViewModel<T> : ImportSourceViewModel
        where T : ImportableViewModel
    {
        private readonly ItemsDatabaseProvider<T> _sourceProvider;

        public ImportSourceDatabaseViewModel(ItemsDatabaseProvider<T> sourceProvider)
        {
            _sourceProvider = sourceProvider;
            ConnectionBusyService = BusyManager.Create();
            (Name, Host) = sourceProvider.GetConnectionInfo();

            TestConnectionCommand = CommandsManager.Create(async () => await TestConnectionAsync().ConfigureAwait(false));
        }

        public IBusyService ConnectionBusyService { get; }

        public string Name { get; }

        public string Host { get; }

        public bool? CanConnect { get; private set; } = null;

        public ICommand TestConnectionCommand { get; set; }

        public override async Task RefreshAsync() => await TestConnectionAsync().ConfigureAwait(false);

        private async Task TestConnectionAsync() => await ConnectionBusyService.WaitIndeterminateAsync(() =>
        {
            CanConnect = _sourceProvider.CanConnect();

            Messenger.Default.Send(new DatabaseConnectionCheckedMessage(Name, Host, CanConnect.IsTrue()));
        }).ConfigureAwait(false);

        public override async Task<bool> InitializeAsync() => await Task.FromResult(!CanConnect.IsFalse()).ConfigureAwait(false);

        public override bool IsEnabled() => !CanConnect.IsFalse();

        public override (IEnumerable<T1>, bool) LoadItems<T1>() => ((IEnumerable<T1>)_sourceProvider.ProvideItems(), CanConnect.IsFalse());
    }
}
