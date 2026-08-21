using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions
{
    public static IEnumerable<GameObject> EnumerateChildren(this GameObject gameObject)
    {
        var transform = gameObject.transform;
        var childCount = transform.childCount;
        for (var i = 0; i < childCount; i++)
        {
            yield return transform.GetChild(i).gameObject;
        }
    }

    /// <summary>
    /// Returns first level children of game object
    /// </summary>
    /// <returns></returns>
    public static GameObject[] GetAllChildren(this GameObject gameObject)
    {
        var transform = gameObject.transform;
        var childCount = transform.childCount;
        var children = new GameObject[childCount];

        for (var i = 0; i < childCount; i++)
        {
            children[i] = transform.GetChild(i).gameObject;
        }

        return children;
    }

    public static IEnumerable<GameObject> GetInactiveChildren(this GameObject gameObject)
    {
        var transform = gameObject.transform;
        var childCount = transform.childCount;
        for (var i = 0; i < childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            if (!child.activeInHierarchy)
            {
                yield return child;
            }
        }
    }
    
    public static IEnumerable<GameObject> GetActiveChildren(this GameObject gameObject)
    {
        var transform = gameObject.transform;
        var childCount = transform.childCount;
        for (var i = 0; i < childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            if (child.activeInHierarchy)
            {
                yield return child;
            }
        }
    }
}
