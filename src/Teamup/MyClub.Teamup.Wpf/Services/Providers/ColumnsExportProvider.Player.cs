// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Converters;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.Settings;
using MyNet.CsvHelper.Extensions;
using MyNet.CsvHelper.Extensions.Converters;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Wpf.ViewModels.Export
{


    public class PlayerColumnsExportProvider : ColumnWrappersExportProviderBase<SquadPlayerExportDto>
    {
        public static readonly IEnumerable<ColumnMapping<SquadPlayerExportDto, object?>> DefaultColumns =
        [
            new(x => x.LastName, nameof(MyClubResources.Name)),
            new(x => x.FirstName, nameof(MyClubResources.FirstName)),
            new(x => x.Birthdate, nameof(MyClubResources.Birthdate)),
            new(x => x.Age, nameof(MyClubResources.Age)),
            new(x => x.PlaceOfBirth, nameof(MyClubResources.PlaceOfBirth)),
            new(x => x.Country, nameof(MyClubResources.Nationality), new EnumerationConverter<Country>()),
            new(x => x.Gender, nameof(MyClubResources.Gender), new EnumConverter<GenderType>()),
            new(x => x.Team, nameof(MyClubResources.Team)),
            new(x => x.Category, nameof(MyClubResources.Category), new EnumerationConverter<Category>()),
            new(x => x.Number, nameof(MyClubResources.Number)),
            new(x => x.LicenseNumber, nameof(MyClubResources.LicenseNumber)),
            new(x => x.LicenseState, nameof(MyClubResources.LicenseState), new EnumConverter<LicenseState>()),
            new(x => x.IsMutation, nameof(MyClubResources.IsMutation)),
            new(x => x.FromDate, nameof(MyClubResources.InClubFromDate)),
            new(x => x.Position, nameof(MyClubResources.Position), new EnumerationConverter<Position>()),
            new(x => x.Positions, nameof(MyClubResources.Positions), new CsvPositionsConverter()),
            new(x => x.Laterality, nameof(MyClubResources.Laterality), new EnumConverter<Laterality>()),
            new(x => x.Height, nameof(MyClubResources.Height)),
            new(x => x.Weight, nameof(MyClubResources.Weight)),
            new(x => x.Size, nameof(MyClubResources.Size)),
            new(x => x.ShoesSize, nameof(MyClubResources.ShoesSize)),
            new(x => x.Street, nameof(MyClubResources.Street)),
            new(x => x.PostalCode, nameof(MyClubResources.PostalCode)),
            new(x => x.City, nameof(MyClubResources.City)),
            new(x => x.Email, nameof(MyClubResources.Email)),
            new(x => x.Phone, nameof(MyClubResources.Phone)),
            new(x => x.Emails, nameof(MyClubResources.Emails), new CsvEmailsConverter()),
            new(x => x.Phones, nameof(MyClubResources.Phones), new CsvPhonesConverter()),
            new(x => x.Description, nameof(MyClubResources.Description)),
        ];

        public PlayerColumnsExportProvider() : base(DefaultColumns, ExportPlayersSettings.Default.ColumnsOrder, ExportPlayersSettings.Default.SelectedColumns) { }
    }
}
