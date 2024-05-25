// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyNet.CsvHelper.Extensions;
using MyNet.Observable.Translatables;

namespace MyClub.Teamup.Wpf.Services.Providers.Base
{
    public class ColumnMappingWrapper<T, TMember> : TranslatableString
    {
        public ColumnMapping<T, TMember> ColumnMapping { get; }

        public ColumnMappingWrapper(ColumnMapping<T, TMember> columnMapping) : base(columnMapping.ResourceKey) => ColumnMapping = columnMapping;
    }

    public class ColumnWrappersExportProviderBase<T> : ColumnsExportProvider<T>
    {
        public ColumnWrappersExportProviderBase(IEnumerable<ColumnMapping<T, object?>> defaultColumns, string columnsOrder, string selectedColumns)
            : base(defaultColumns, columnsOrder, selectedColumns) { }

        public IDictionary<ColumnMappingWrapper<T, object?>, bool> ProvideWrappers() => ProvideColumns().ToDictionary(x => new ColumnMappingWrapper<T, object?>(x.Key), x => x.Value);
    }
}
