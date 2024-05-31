// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using MyClub.CrossCutting.Localization;
using MyClub.Plugins.Teamup.Import.File.Converters;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Plugins.Contracts.Dtos;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Excel;
using MyNet.CsvHelper.Extensions.Exceptions;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.IO;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Plugins.Teamup.Import.File.Providers
{
    internal class CompetitionsFileProvider(IReadService readService) : ItemsFileProvider<CompetitionImportDto>
    {
        public static readonly IEnumerable<ColumnMapping<CompetitionImportDto, object?>> DefaultColumns =
        [
            new(x => x.Type, nameof(MyClubResources.Type)),
            new(x => x.Name, nameof(MyClubResources.Name)),
            new(x => x.ShortName, nameof(MyClubResources.ShortName)),
            new(x => x.Category, nameof(MyClubResources.Category)),
            new(x => x.MatchTime, nameof(MyClubResources.DefaultTime)),
            new(x => x.HasExtraTime, nameof(MyClubResources.ExtraTime)),
            new(x => x.RegulationTime, nameof(MyClubResources.RegulationTimeFormat), new CsvHalfFormatConverter()),
            new(x => x.ExtraTime, nameof(MyClubResources.ExtraTimeFormat), new CsvHalfFormatConverter()),
            new(x => x.HasShootouts, nameof(MyClubResources.Shootouts)),
            new(x => x.NumberOfShootouts, nameof(MyClubResources.NumberOfShootouts)),
            new(x => x.ByGamesWon, nameof(MyClubResources.ByGamesWon)),
            new(x => x.ByGamesDrawn, nameof(MyClubResources.ByGamesDrawn)),
            new(x => x.ByGamesLost, nameof(MyClubResources.ByGamesLost)),
            new(x => x.RankingSortingColumns, nameof(MyClubResources.RankingSortingColumns)),
            new(x => x.Labels, nameof(MyClubResources.Labels), new CsvRankingLabelsConverter())
        ];

        private readonly IReadService _readService = readService;

        protected override (IEnumerable<CompetitionImportDto> items, IEnumerable<Exception> exceptions) LoadItems(string filename)
            => _readService.CanRead(filename)
                ? (ExtractCompetitionsFromTmprojFile(filename), Array.Empty<Exception>())
                : ExtractCompetitionsFromFile(filename);

        private List<CompetitionImportDto> ExtractCompetitionsFromTmprojFile(string filename)
        {
            var items = _readService.ReadCompetitionsAsync(filename).Result;

            return items.Select(x =>
            {
                var item = new CompetitionImportDto
                {
                    MatchTime = x.Rules.MatchTime,
                    Type = x.GetType().Name,
                    Logo = x.Logo,
                    Name = x.Name,
                    Category = x.Category.ToString(),
                    ShortName = x.ShortName,
                    ExtraTime = x.Rules.MatchFormat.ExtraTime is not null
                                ? new HalfFormatImportDto
                                {
                                    Number = x.Rules.MatchFormat.ExtraTime.Number,
                                    Duration = x.Rules.MatchFormat.ExtraTime.Duration
                                } : null,
                    RegulationTime = new HalfFormatImportDto
                    {
                        Duration = x.Rules.MatchFormat.RegulationTime.Duration,
                        Number = x.Rules.MatchFormat.RegulationTime.Number,
                    },
                    HasExtraTime = x.Rules.MatchFormat.ExtraTimeIsEnabled,
                    HasShootouts = x.Rules.MatchFormat.ShootoutIsEnabled,
                    NumberOfShootouts = x.Rules.MatchFormat.NumberOfPenaltyShootouts,
                    ByGamesDrawn = (x as League)?.Rules.RankingRules.PointsByGamesDrawn,
                    ByGamesWon = (x as League)?.Rules.RankingRules.PointsByGamesWon,
                    ByGamesLost = (x as League)?.Rules.RankingRules.PointsByGamesLost,
                    RankingSortingColumns = (x as League)?.Rules.RankingRules.SortingColumns.Select(y => y.ToString()),
                    Labels = (x as League)?.Rules.RankingRules.Labels.ToDictionary(y => (y.Key.Min, y.Key.Max), y => new RankLabelImportDto
                    {
                        Color = y.Value.Color,
                        Description = y.Value.Description,
                        Name = y.Value.Name,
                        ShortName = y.Value.ShortName,
                    }),
                };

                return item;
            }).ToList();
        }

        private static (ICollection<CompetitionImportDto>, ICollection<Exception>) ExtractCompetitionsFromFile(string filename)
        {
            var exceptions = new List<Exception>();
            var configuration = CsvConfigurations.GetConfigurationWithNoThrowException(exceptions);

            using var reader = FileExtensionInfoProvider.Excel.IsValid(filename)
                ? new CsvReader(new ExcelParser(filename, configuration))
                : FileExtensionInfoProvider.Csv.IsValid(filename)
                ? new CsvReader(new StreamReader(filename), configuration)
                : throw new TranslatableException(MyClubResources.FileMustBeExcelOrCsvError);

            reader.Context.RegisterClassMap(new DynamicClassMap<CompetitionImportDto>(DefaultColumns, false, false));

            var competitions = reader.GetRecords<CompetitionImportDto>().ToList();

            var index = 1;
            var convertedCompetitions = competitions.Select(x =>
            {
                var competition = new CompetitionImportDto();

                trySetValue(() => competition.Name = x.Name, nameof(CompetitionImportDto.Name), x.Name);
                trySetValue(() => competition.ShortName = x.ShortName, nameof(CompetitionImportDto.ShortName), x.ShortName);
                trySetValue(() => competition.MatchTime = x.MatchTime, nameof(CompetitionImportDto.MatchTime), x.MatchTime);
                trySetValue(() => competition.Type = x.Type, nameof(CompetitionImportDto.Type), x.Type);
                trySetValue(() => competition.Category = x.Category, nameof(CompetitionImportDto.Category), x.Category);
                trySetValue(() => competition.ByGamesDrawn = x.ByGamesDrawn, nameof(CompetitionImportDto.ByGamesDrawn), x.ByGamesDrawn);
                trySetValue(() => competition.ByGamesWon = x.ByGamesWon, nameof(CompetitionImportDto.ByGamesWon), x.ByGamesWon);
                trySetValue(() => competition.ByGamesLost = x.ByGamesLost, nameof(CompetitionImportDto.ByGamesLost), x.ByGamesLost);
                trySetValue(() => competition.RankingSortingColumns = x.RankingSortingColumns, nameof(CompetitionImportDto.RankingSortingColumns), x.RankingSortingColumns);
                trySetValue(() => competition.Labels = x.Labels, nameof(CompetitionImportDto.Labels), x.Labels);
                trySetValue(() => competition.HasExtraTime = x.HasExtraTime, nameof(MyClubResources.ExtraTime), x.HasExtraTime);
                trySetValue(() => competition.HasShootouts = x.HasShootouts, nameof(MyClubResources.Shootouts), x.HasShootouts);
                trySetValue(() => competition.ExtraTime = x.ExtraTime, nameof(MyClubResources.ExtraTimeFormat), x.ExtraTime);
                trySetValue(() => competition.RegulationTime = x.RegulationTime, nameof(MyClubResources.RegulationTimeFormat), x.RegulationTime);
                trySetValue(() => competition.NumberOfShootouts = x.NumberOfShootouts, nameof(MyClubResources.NumberOfShootouts), x.NumberOfShootouts);

                index++;
                return competition;

                void trySetValue<T>(Action action, string columnHeader, T value)
                {
                    try
                    {
                        action();
                    }
                    catch (TranslatableException e)
                    {
                        exceptions?.Add(new ImportValueException(index, columnHeader, value, e.Parameters is not null ? e.ResourceKey.Translate()?.FormatWith(e.Parameters) : e.ResourceKey.Translate(), e));
                    }
                    catch (Exception e)
                    {
                        exceptions?.Add(new ImportValueException(index, columnHeader, value, e.Message, e));
                    }
                }

            }).ToList();
            return (convertedCompetitions, exceptions);
        }
    }
}
