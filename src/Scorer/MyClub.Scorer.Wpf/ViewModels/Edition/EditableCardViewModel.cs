// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.Commands;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableCardViewModel : EditableObject
    {
        public EditableCardViewModel() => ClearPlayerCommand = CommandsManager.Create(() => Player = null, () => Player is not null);


        [Display(Name = nameof(Minute), ResourceType = typeof(MyClubResources))]
        public int Minute { get; set; } = 1;

        public bool MinuteIsEnabled { get; set; }

        [Display(Name = nameof(Color), ResourceType = typeof(MyClubResources))]
        [OnChangedMethod(nameof(OnColorChanged))]
        public CardColor? Color { get; set; }

        [Display(Name = nameof(Infraction), ResourceType = typeof(MyClubResources))]
        public CardInfraction Infraction { get; set; }

        [Display(Name = nameof(Description), ResourceType = typeof(MyClubResources))]
        public string? Description { get; set; }

        [Display(Name = nameof(Scorer), ResourceType = typeof(MyClubResources))]
        public PlayerViewModel? Player { get; set; }

        public ICommand ClearPlayerCommand { get; }

        public CardDto ToDto() => new()
        {
            PlayerId = Player?.Id,
            Color = Color.GetValueOrDefault(),
            Infraction = Infraction,
            Description = Description,
            Minute = MinuteIsEnabled ? Minute : null,
        };

        // Fix a bug : When UI content change (switch tab), SelectedItem send a Null value and remove previous value
        private void OnColorChanged(CardColor? oldValue, CardColor? newValue)
        {
            if (oldValue.HasValue && !newValue.HasValue)
                Color = oldValue.Value;
        }
    }
}
