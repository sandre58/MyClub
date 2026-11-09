// -----------------------------------------------------------------------
// <copyright file="CascadeAggregateCopy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Reflection;

namespace MyClub.PlayUp.Application.Stages.CascadeReplace;

/// <summary>
/// Scratch copy of a stage or match graph so a cascade plan can replay selection
/// without writing the caller's aggregates.
/// </summary>
internal static class CascadeAggregateCopy
{
    private const string DomainNamespace = "MyClub.PlayUp.Domain";

    private static readonly MethodInfo MemberwiseCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// Returns a deep copy of a domain aggregate. Value objects and types outside the domain are shared.
    /// </summary>
    /// <typeparam name="T">Aggregate type.</typeparam>
    /// <param name="source">The instance to copy.</param>
    /// <returns>A distinct instance with the same identity and state.</returns>
    public static T Copy<T>(T source)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return (T)CopyObject(source, new Dictionary<object, object>(ReferenceEqualityComparer.Instance))!;
    }

    private static object? CopyObject(object? value, Dictionary<object, object> seen)
    {
        if (value is null || value is string || value.GetType().IsValueType)
        {
            return value;
        }

        if (seen.TryGetValue(value, out var existing))
        {
            return existing;
        }

        var type = value.GetType();
        if (type.IsArray)
        {
            var array = (Array)value;
            var elementType = type.GetElementType()!;
            var copy = Array.CreateInstance(elementType, array.Length);
            seen[value] = copy;
            for (var index = 0; index < array.Length; index++)
            {
                copy.SetValue(CopyObject(array.GetValue(index), seen), index);
            }

            return copy;
        }

        if (value is IList list && type.GetConstructor(Type.EmptyTypes) is not null)
        {
            var copy = (IList)Activator.CreateInstance(type)!;
            seen[value] = copy;
            foreach (var item in list)
            {
                copy.Add(CopyObject(item, seen));
            }

            return copy;
        }

        if (value is IDictionary map && type.GetConstructor(Type.EmptyTypes) is not null)
        {
            var copy = (IDictionary)Activator.CreateInstance(type)!;
            seen[value] = copy;
            foreach (DictionaryEntry entry in map)
            {
                var key = CopyObject(entry.Key, seen);
                if (key is null)
                {
                    continue;
                }

                copy.Add(key, CopyObject(entry.Value, seen));
            }

            return copy;
        }

        if (!InDomain(type))
        {
            return value;
        }

        var clone = MemberwiseCloneMethod.Invoke(value, null)!;
        seen[value] = clone;
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsStatic)
                {
                    continue;
                }

                var fieldValue = field.GetValue(value);
                if (fieldValue is null || fieldValue is string || fieldValue.GetType().IsValueType)
                {
                    continue;
                }

                field.SetValue(clone, CopyObject(fieldValue, seen));
            }
        }

        return clone;
    }

    private static bool InDomain(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.Namespace?.StartsWith(DomainNamespace, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }
}
