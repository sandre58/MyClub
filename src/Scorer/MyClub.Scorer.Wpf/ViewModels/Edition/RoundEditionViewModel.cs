// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.BuildAssistant;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal enum RoundFormat
    {
        OneLeg,

        TwoLeg,

        Replay,

        NumberOfWins
    }

    internal class RoundEditionViewModel : EntityEditionViewModel<Round, RoundDto, RoundService>
    {
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly ObservableCollection<IVirtualTeamViewModel> _availableTeams = [];
        //private readonly Dictionary<RoundFormat, IRoundFormatViewModel> _roundFormatViewModels = new()
        //{
        //    { RoundFormat.OneLeg, new DatesSchedulingAsSoonAsPossibleViewModel() },
        //    { RoundFormat.TwoLeg, new DatesSchedulingManualViewModel() },
        //    { RoundFormat.Replay, new DatesSchedulingAutomaticViewModel() },
        //    { RoundFormat.NumberOfWins, new DatesSchedulingAutomaticViewModel() },
        //};

        public RoundEditionViewModel(RoundService roundService, StadiumsProvider stadiumsProvider) : base(roundService)
        {
            _stadiumsProvider = stadiumsProvider;
            SchedulingParameters = new EditableSchedulingParametersViewModel(new ObservableSourceProvider<IEditableStadiumViewModel>(stadiumsProvider.Items.ToObservableChangeSet().Transform(x => (IEditableStadiumViewModel)x)));

            RemoveRoundStageCommand = CommandsManager.CreateNotNull<EditableRoundStageViewModel>(x => RoundStages.Remove(x), x => x.CanRemove);
            AddRoundStageCommand = CommandsManager.Create(() => RoundStages.Add(new EditableRoundStageViewModel(Guid.Empty, MyClubResources.NewStage, "*", true)), () => CanAddRoundStage);

            Disposables.AddRange(
                [
                    this.WhenPropertyChanged(x => x.UseDefaultRules, false).Subscribe(_ =>
                    {
                        UseDefaultRules.IfTrue(() => MatchRules.Load(CrudService.GetDefaultRules()));
                        UpdateSchedulingState();
                    }),
                    this.WhenPropertyChanged(x => x.UseDefaultSchedulingParameters, false).Subscribe(_ =>
                    {
                        UseDefaultSchedulingParameters.IfTrue(() => SchedulingParameters.Load(CrudService.GetDefaultSchedulingParameters()));
                        UpdateSchedulingState();
                    }),
                    SchedulingParameters.WhenPropertyChanged(x => x.AsSoonAsPossible, false).Subscribe(_ => UpdateSchedulingState()),
                    SchedulingParameters.WhenPropertyChanged(x => x.UseHomeVenue, false).Subscribe(_ => UpdateSchedulingState()),
                    SchedulingParameters.VenueRules.Rules.ToObservableChangeSet().Subscribe(_ => UpdateSchedulingState()),
                    SchedulingParameters.DateRules.Rules.ToObservableChangeSet().Subscribe(_ => UpdateSchedulingState()),
                    RoundStages.ToObservableChangeSet().Subscribe(_ => CanAddRoundStage = ItemId.HasValue && (CrudService.GetById(ItemId.Value)?.CanAddStage() ?? false))
                ]);
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public IRoundsStageViewModel? Stage { get; private set; }

        public ObservableCollection<EditableRoundStageViewModel> RoundStages { get; } = [];

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public virtual string Name { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]

        public virtual string ShortName { get; set; } = string.Empty;

        public bool UseDefaultSchedulingParameters { get; set; }

        public bool UseDefaultRules { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanAddRoundStage { get; private set; }

        public EditableMatchRulesViewModel MatchRules { get; } = new();

        public EditableSchedulingParametersViewModel SchedulingParameters { get; }

        public ICommand RemoveRoundStageCommand { get; }

        public ICommand AddRoundStageCommand { get; }

        public void New(IRoundsStageViewModel stage, DateTime? date = null)
        {
            UseDefaultSchedulingParameters = true;
            UseDefaultRules = true;
            Stage = stage;
            New();
        }

        public void Load(RoundViewModel round)
        {
            Stage = round.Stage;
            _availableTeams.Set(round.GetAvailableTeams());
            Load(round.Id);
        }

        public void Load(RoundStageViewModel roundStage) => Load(roundStage.Stage);

        protected override void ResetItem()
        {
            var defaultValues = CrudService.New(Stage?.Id);
            ShortName = defaultValues.ShortName.OrEmpty();
            Name = defaultValues.Name.OrEmpty();
            UseDefaultSchedulingParameters = true;
            UseDefaultRules = true;
            SchedulingParameters.Load(CrudService.GetDefaultSchedulingParameters());
            MatchRules.Load(CrudService.GetDefaultRules());
        }

        protected override RoundDto ToDto()
            => new()
            {
                Id = ItemId,
                StageId = Stage?.Id,
                Name = Name,
                ShortName = ShortName,
                MatchRules = UseDefaultRules ? null : MatchRules.Create(),
                SchedulingParameters = UseDefaultSchedulingParameters ? null : SchedulingParameters.Create(),
                Stages = RoundStages.Select(x => new RoundStageDto
                {
                    Id = x.Id == Guid.Empty ? null : x.Id,
                    Date = x.CurrentDate.ToUtcOrDefault(),
                    IsPostponed = x.PostponedState == PostponedState.UnknownDate,
                    PostponedDate = x.PostponedState == PostponedState.SpecifiedDate ? x.PostponedDateTime.ToUtc() : null,
                    ScheduleStadiumsAutomatic = x.CanScheduleStadiumsAutomatic && x.ScheduleStadiumsAutomatic,
                    ScheduleAutomatic = x.CanScheduleAutomatic && x.ScheduleAutomatic
                }).ToList()
            };

        protected override void RefreshFrom(Round item)
        {
            CanAddRoundStage = item.CanAddStage();
            Name = item.Name;
            ShortName = item.ShortName;
            UseDefaultSchedulingParameters = !item.SchedulingParameters.IsInherited;
            UseDefaultRules = !item.MatchRules.IsInherited;
            SchedulingParameters.Load(item.SchedulingParameters.Value.OrThrow());
            MatchRules.Load(item.MatchRules.Value.OrThrow());
            RoundStages.Set(item.Stages.Select(x =>
            {
                var newItem = new EditableRoundStageViewModel(x.Id, RoundViewModel.ComputeRoundStageName(x), RoundViewModel.ComputeRoundStageShortName(x), item.Format.CanRemoveStage(x));
                newItem.CurrentDate.Load(x.OriginDate);
                if (x.OriginDate == x.Date)
                    newItem.PostponedDateTime.Clear();
                else
                    newItem.PostponedDateTime.Load(x.Date);
                newItem.PostponedState = !newItem.PostponedDateTime.HasValue && !x.IsPostponed ? PostponedState.None : newItem.PostponedDateTime.HasValue ? PostponedState.SpecifiedDate : PostponedState.UnknownDate;

                newItem.Matches.Set(x.Matches.OrderBy(x => x.Date).Select(x =>
                {
                    var result = new EditableMatchViewModel(x.Id, _availableTeams, _stadiumsProvider, false);
                    result.CurrentDate.Load(x.Date);
                    result.HomeTeam = result.AvailableTeams.GetById(x.HomeTeam.Id);
                    result.AwayTeam = result.AvailableTeams.GetById(x.AwayTeam.Id);
                    result.StadiumSelection.Select(x.Stadium?.Id);

                    return result;
                }));
                newItem.ScheduleAutomatic = false;
                newItem.ScheduleStadiumsAutomatic = false;
                return newItem;
            }));
            UpdateSchedulingState();
        }

        private void UpdateSchedulingState() => RoundStages.ForEach(x =>
        {
            x.CanScheduleAutomatic = SchedulingParameters.CanScheduleAutomatic();
            x.CanScheduleStadiumsAutomatic = SchedulingParameters.CanScheduleVenuesAutomatic();

            if (!x.CanScheduleAutomatic)
                x.ScheduleAutomatic = false;

            if (!x.CanScheduleStadiumsAutomatic)
                x.ScheduleStadiumsAutomatic = false;
        });
    }
}
