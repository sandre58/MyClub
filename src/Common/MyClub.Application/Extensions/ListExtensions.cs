// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Application.Dtos;
using MyClub.Domain;

namespace MyClub.Application.Extensions
{
    public static class ListExtensions
    {
        public static void UpdateFrom<TSource, TDestination>(this IList<TDestination> destination,
            IEnumerable<TSource> source,
            Action<TSource> add,
            Action<TDestination> remove,
            Action<TDestination, TSource> update)
            where TDestination : Entity
            where TSource : EntityDto
        {
            var sourceList = source.ToList();

            // Delete
            var toDelete = destination.Where(x => !sourceList.Exists(y => x.Id == y.Id)).ToList();
            toDelete.ForEach(remove);

            // Update
            var toUpdate = destination.Where(x => sourceList.Exists(y => x.Id == y.Id)).ToList();
            toUpdate.ForEach(x => update(x, sourceList.First(y => x.Id == y.Id)));

            // Add
            var toAdd = sourceList.Where(x => !destination.Any(y => x.Id == y.Id)).ToList();
            toAdd.ForEach(add);
        }
    }
}
