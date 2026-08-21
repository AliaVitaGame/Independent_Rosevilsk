using System;
using System.Collections.Generic;

public static class CollectionsExtensions
{
    private static readonly Random Random = new();

    public static T GetRandomElement<T>(this IReadOnlyList<T> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (collection.Count == 0)
        {
            throw new ArgumentException("Collection cannot be empty.");
        }

        return collection[Random.Next(collection.Count)];
    }
}
