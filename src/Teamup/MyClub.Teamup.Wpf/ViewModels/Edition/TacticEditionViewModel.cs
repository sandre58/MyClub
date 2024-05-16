// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Wpf.Extensions;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class TacticEditionViewModel : EntityEditionViewModel<Tactic, TacticDto, TacticService>
    {
        [Display(Name = nameof(Label), ResourceType = typeof(MyClubResources))]
        public string? Label { get; set; }

        [Display(Name = nameof(Code), ResourceType = typeof(MyClubResources))]
        public string? Code { get; set; }

        [Display(Name = nameof(Description), ResourceType = typeof(MyClubResources))]
        public string? Description { get; set; }

        public StringListEditionViewModel InstructionsViewModel { get; } = new(MyClubResources.Instructions);

        public IReadOnlyCollection<EditableTacticPositionViewModel>? AllPositions { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowInstructions { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public EditableTacticPositionViewModel? EditingPosition { get; private set; }

        [DoNotCheckEquality]
        public IEnumerable? SelectedPositions { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Color? HomeColor { get; private set; }

        public ICommand RemoveAllPositionsCommand { get; }

        public ICommand ResetAllPositionsCommand { get; }

        public ICommand ShowPositionInstructionsCommand { get; }

        public TacticEditionViewModel(
            ProjectInfoProvider projectInfoProvider,
            TacticService tacticService)
            : base(tacticService)
        {
            projectInfoProvider.WhenClubPropertyChanged(x => x.HomeColor, UpdateHomeColor);
            projectInfoProvider.WhenProjectChanged(x => UpdateHomeColor(x?.Club.HomeColor));
            UpdateHomeColor(projectInfoProvider.GetCurrentProject()?.Club.HomeColor);

            RemoveAllPositionsCommand = CommandsManager.Create(() => SelectedPositions = null, () => SelectedPositions is not null);
            ResetAllPositionsCommand = CommandsManager.Create(() => AllPositions?.ForEach(x => x.ResetOffset()));
            ShowPositionInstructionsCommand = CommandsManager.CreateNotNull<EditableTacticPositionViewModel>(OpenPositionInstructions);
        }

        private void UpdateHomeColor(string? colorName) => HomeColor = colorName.ToColor();

        protected override TacticDto ToDto()
        => new()
        {
            Id = ItemId,
            Label = Label,
            Code = Code,
            Description = Description,
            Instructions = InstructionsViewModel.Items.Select(x => x.Value).NotNull().ToList(),
            Positions = new List<TacticPositionDto>(SelectedPositions?.OfType<EditableTacticPositionViewModel>().Select(x => new TacticPositionDto
            {
                Id = x.Id,
                Number = x.Number,
                Position = x.Position,
                OffsetX = x.OffsetX,
                OffsetY = x.OffsetY,
                Instructions = x.InstructionsViewModel.Items.Select(x => x.Value).NotNull().ToList()
            }).ToArray() ?? [])
        };

        protected override void RefreshFrom(Tactic item)
        {
            ShowInstructions = false;
            Label = item.Label;
            Code = item.Code;
            Description = item.Description;
            InstructionsViewModel.SetSource(item.Instructions.ToObservableCollection());
            UpdatePositions(item.Positions.Select(x =>
            new TacticPositionDto
            {
                Id = x.Id,
                Number = x.Number,
                OffsetX = x.OffsetX,
                OffsetY = x.OffsetY,
                Instructions = [.. x.Instructions],
                Position = x.Position
            }));
        }

        protected override void ResetItem()
        {
            var dto = CrudService.NewTactic();

            ShowInstructions = false;
            Label = dto.Label;
            Code = dto.Code;
            Description = dto.Description;
            InstructionsViewModel.SetSource(dto.Instructions?.ToObservableCollection() ?? []);
            UpdatePositions(dto.Positions);
        }

        public void UpdatePositions(IEnumerable<TacticPositionDto>? positions)
        {
            AllPositions = Enumeration.GetAll<Position>().Select(x =>
            {
                var foundPosition = positions?.FirstOrDefault(y => y.Position is not null && x == y.Position);
                var editablePosition = new EditableTacticPositionViewModel(x, foundPosition?.Id)
                {
                    OffsetY = foundPosition?.OffsetY ?? 0,
                    OffsetX = foundPosition?.OffsetX ?? 0,
                };
                editablePosition.Number.Value = foundPosition?.Number;
                editablePosition.InstructionsViewModel.SetSource(foundPosition?.Instructions.ToObservableCollection() ?? []);

                return editablePosition;
            }).ToList().AsReadOnly();
            SelectedPositions = positions is not null
                ? AllPositions.Where(x => positions.Select(y => y.Position).Contains(x.Position)).OrderBy(x => x.Position).ToList()
                : null;
        }

        public void OpenPositionInstructions(EditableTacticPositionViewModel editableTacticPositionViewModel)
        {
            EditingPosition = editableTacticPositionViewModel;
            ShowInstructions = true;
        }

        protected virtual void OnShowInstructionsChanged()
        {
            if (!ShowInstructions) EditingPosition = null;
        }
    }
}
