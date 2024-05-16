// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.Wpf.Controls;
using MyNet.Observable;
using MyClub.Teamup.Wpf.Controls;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.Shell
{
    internal class SearchViewModel : ObservableObject, ISuggestionProvider
    {
        private readonly NavigableItemsProvider _navigableItemsProvider;

        public SearchViewModel(NavigableItemsProvider navigableItemsProvider)
        {
            _navigableItemsProvider = navigableItemsProvider;
            Disposables.Add(this.WhenPropertyChanged(x => x.SelectedItem).Subscribe(x =>
            {
                if (x.Value is null) return;

                x.Value.Open();
                TextSearch = string.Empty;
            }));
        }

        public string TextSearch { get; set; } = string.Empty;

        public ISearchableItem? SelectedItem { get; set; }

        public IEnumerable GetSuggestions(string? filter)
            => _navigableItemsProvider.Source.Where(x => !string.IsNullOrEmpty(filter) && x.SearchText.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }
}
